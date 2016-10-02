using ComfoAirClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ComfoAir
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            tb_OutFileName.Text = DateTime.Now.ToString("yyyyMMdd") + "_ZehnderRS232.dat";
        }

        // ===================================================================================
        // MAIN VARIABLES
        // ===================================================================================

        BinaryWriter bw_output;
        bool bRawDumpToFile = false;
        FileMode outFileMode = FileMode.Append;
        string outputFile = "";

        // ===================================================================================
        // SERIAL PORT CODE
        // ===================================================================================

        private SerialPort _serialPort = new SerialPort();
        private byte[] serialBuffer;                // Buffer for reading from serial port
        private byte[] processBuffer;               // Buffer to hold unprocessed data between serial port reads
        private int processSize = 0;

        private enum fontstyles { NORMAL, ITALIC, BOLD, BOLDITALIC, UNDERLINE, BOLDUNDERLINE };

        private void populateSerialPorts()
        {
            // Populate the COM ports again
            cb_SerialPort.Items.Clear();
            foreach (string sPort in SerialPort.GetPortNames())
            {
                // Urghhh... serial ports are just implemented like crap in .NET. The GetPortNames() just
                // returns all serial ports defined in the registry, including those that are "inactive"
                // in device manager. If like me, you have 200 Arduino COM ports, that will just mess up
                // the results. 
                //
                // Workaround: we try to open every port that we find, listing only those that we can
                // access.
                try
                {
                    SerialPort tempport = new SerialPort(sPort);
                    tempport.Open();
                    // If we get to this code, we can open the port. Close & add it... 
                    tempport.Close();
                    cb_SerialPort.Items.Add(sPort);
                }
                catch (Exception ex)
                {
                    // Let it be known to the world that we do nothing here.
                }
            }
            // Did we get items?
            if (cb_SerialPort.Items.Count == 0)
            {
                cb_SerialPort.Items.Add("No serial devices found!");
                cb_SerialPort.SelectedIndex = 0;
                cb_SerialPort.IsEnabled = false;
                but_Start.IsEnabled = false;
                but_Stop.IsEnabled = false;
                cb_SerialAdvanced.IsEnabled = false;
            }
            else
            {
                cb_SerialPort.SelectedIndex = 0;
                cb_SerialPort.IsEnabled = true;
                but_Start.IsEnabled = true;
                but_Stop.IsEnabled = false;
                cb_SerialAdvanced.IsEnabled = true;
            }
        }

        private void startSerialMonitoring()
        {
            // Initialize serial port buffer for this Serial Port
            serialBuffer = new byte[_serialPort.ReadBufferSize];
            // Reinitialize the process buffer, with some margin for reading data... 
            dropProcessBuffer();

            // Configure serial port
            string serOptions = "";
            switch (cb_SerialOptions.SelectedIndex) {
                case 1: // 8N1
                    _serialPort.DataBits = 8;
                    _serialPort.Parity = Parity.None;
                    _serialPort.StopBits = StopBits.One;
                    serOptions = "8N1";
                    break;

                default: // 7N1
                    _serialPort.DataBits = 7;
                    _serialPort.Parity = Parity.None;
                    _serialPort.StopBits = StopBits.One;
                    serOptions = "7N1";
                    break;
            }
            _serialPort.BaudRate = Convert.ToInt32(tb_Baudrate.Text);
            _serialPort.Handshake = Handshake.None;
            _serialPort.PortName = cb_SerialPort.Text;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            // Start output
            updateTextBox(tb_Output, "--- SERIAL PORT: " + _serialPort.PortName + " (" + serOptions + ")---\n", fontstyles.BOLD);
            updateTextBox(tb_RawOutput, "\n--- SERIAL PORT: " + _serialPort.PortName + " (" + serOptions + ")---\n", fontstyles.BOLD);
            updateTextBox(tb_RawOutput, "");   // New textblock to hold the raw data, in the normal style...

            // Start serial port monitoring
            _serialPort.Open();

            // UI updates
            but_Stop.IsEnabled = true;
            but_Start.IsEnabled = false;
        }

        private void stopSerialMonitoring()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            // UI updates
            updateTextBox(tb_Output, "--- END OF SERIAL PORT ---\n", fontstyles.BOLD);
            updateTextBox(tb_RawOutput, "\n--- END OF SERIAL PORT ---\n", fontstyles.BOLD);
            but_Stop.IsEnabled = false;
            but_Start.IsEnabled = true;
        }

        // EVENT HANDLER for serial data
        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                // There is no accurate method for checking how many bytes are read 
                // unless you check the return from the Read method 
                // See also: http://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
                int bytesRead = _serialPort.Read(serialBuffer, 0, serialBuffer.Length);

                // Retrieve only the read data from our buffer for further processing
                // This is to avoid a bunch of redundant zeroes when parsing... 
                byte[] readBuffer = new byte[bytesRead];
                Array.Copy(serialBuffer, 0, readBuffer, 0, bytesRead);

                // Dump raw data
                rawDataDump(readBuffer);

                // Prepare for parsing; first check if there is still outstanding data to parse...
                byte[] truncBuffer = new byte[bytesRead + processSize];
                if (processSize > 0)
                {
                    // Copy outstanding data to new buffer
                    Array.Copy(processBuffer, 0, truncBuffer, 0, processSize);
                    // Copy new serial data
                    Array.Copy(readBuffer, 0, truncBuffer, processSize, bytesRead);
                    // Clear "cue" of waiting data... 
                    dropProcessBuffer();
                }
                else
                {
                    // Just copy directly to our truncbuffer
                    Array.Copy(serialBuffer, 0, truncBuffer, 0, bytesRead);
                }


                // Feed data to parser
                int processedOffset = 0;
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    processedOffset = parseAndShowData(truncBuffer);                   
                }));


                // Next, retain all data past the "processedOffset"; this could be a partial command of which we get
                // the rest in a next serial port read... 
                if (processedOffset < truncBuffer.Length)
                {
                    int lengthToStore = (truncBuffer.Length - processedOffset);
                    // Does it fit in our processBuffer ?
                    if (processSize + lengthToStore > processBuffer.Length)
                    {
                        dropProcessBuffer();
                    }
                    Array.Copy(truncBuffer, processedOffset, processBuffer, processSize, lengthToStore);
                    processSize += lengthToStore;
                }
            }


        }

        private void dropProcessBuffer()
        {
            // Drop earlier data
            processBuffer = new byte[10 * _serialPort.ReadBufferSize];
            processSize = 0;
        }


    // ===================================================================================
    // DATA PROCESSING CODE
    // ===================================================================================

    private int parseAndShowData(byte[] inputBytes)
        {
            // Parse the file into Zehnder commands
            ZehnderParser zParser = new ZehnderParser(inputBytes);
            List<ZehnderCommand> cmds = zParser.parseResult;

            // Output all data found
            foreach (ZehnderCommand cmd in cmds)
            {
                //updateTextBox(tb_Output, cmd.ToString());
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    addToTextBox(tb_Output, cmd.ToTable());
                    addToTextBox(tb_Output, new BlockUIContainer(new Separator()));
                }));
            }
            // Return last parsed position in the supplied buffer
            return zParser.parseOffset;

        }

        // ===================================================================================
        // DATA VISUALISATION CODE
        // ===================================================================================

        private void rawDataDump(byte[] data)
        {
            // Update UI
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                appendToTextBox(tb_RawOutput, ByteArrayToHexString(data));
            }));

            // Save data to file?
            if (bRawDumpToFile)
            {
                if (bw_output == null)
                {
                    raw_OpenFile();
                }
                if (bw_output != null)
                {
                    // Dump data
                    bw_output.Write(data);
                }
            }

        }

        private void updateTextBox(TextBox tb, string text)
        // Update a regular TextBox
        {
            Dispatcher.Invoke(new ThreadStart(delegate {
                tb.Text += text;
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
                tb.ScrollToEnd();
            }));
        }


        private void updateTextBox(FlowDocumentReader fdr, string text, fontstyles fstyles = fontstyles.NORMAL)
        // Update a FlowDocumentReader
        {
            // Create a new paragraph of text to add to the FDR
            Paragraph paragraph = new Paragraph();

            switch (fstyles)
            {
                case fontstyles.NORMAL:
                    paragraph.Inlines.Add(new Run(text));
                    break;
                case fontstyles.BOLD:
                    paragraph.Inlines.Add(new Bold(new Run(text)));
                    break;
                case fontstyles.BOLDITALIC:
                    paragraph.Inlines.Add(new Italic(new Bold(new Run(text))));
                    break;
                case fontstyles.BOLDUNDERLINE:
                    paragraph.Inlines.Add(new Italic(new Bold(new Run(text))));
                    break;
                case fontstyles.ITALIC:
                    paragraph.Inlines.Add(new Italic(new Run(text)));
                    break;
                case fontstyles.UNDERLINE:
                    paragraph.Inlines.Add(new Underline(new Run(text)));
                    break;
                default:
                    break;
            }
            // Now add it to the FDR
            addToTextBox(fdr, paragraph);
        }

        private void addToTextBox(FlowDocumentReader fdr, Block tBlock)
        {
            // Check if document exists in FlowDocumentReader, otherwise create one
            FlowDocument document = checkDocument(fdr);
            
            // Add a new textblock
            document.Blocks.Add(tBlock);

            // Scroll to bottom
            ScrollViewer scroller = FindScroll(fdr as Visual);
            if (scroller != null)
                (scroller as ScrollViewer).ScrollToBottom();
        }

        private void appendToTextBox(FlowDocumentReader fdr, string text)
        {
            // Retrieve last text block and use that as paragraph
            FlowDocument document = checkDocument(fdr);
            Block lastBlock = document.Blocks.LastBlock;
            if (lastBlock == null)
            {
                // Create a new text block
                Paragraph paragraph = new Paragraph();
                document.Blocks.Add(paragraph);
                lastBlock = document.Blocks.LastBlock;
            }
            lastBlock.ContentEnd.InsertTextInRun(text);

            ScrollViewer scroller = FindScroll(fdr as Visual);
            if (scroller != null)
                (scroller as ScrollViewer).ScrollToBottom();
        }


        private FlowDocument checkDocument(FlowDocumentReader fdr)
        // Check if document exists in FlowDocumentReader, otherwise create one
        {
            FlowDocument document = fdr.Document;
            if (document == null)
            {
                document = new FlowDocument();
                document.IsHyphenationEnabled = false;
                document.PagePadding = new Thickness(0);
                Style parStyle = new Style(typeof(Paragraph));
                parStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0)));
                parStyle.Setters.Add(new Setter(Block.FontFamilyProperty, new FontFamily("Segoe UI")));
                parStyle.Setters.Add(new Setter(Block.FontSizeProperty, 12d));
                document.Resources.Add(typeof(Paragraph), parStyle);
                Style tabStyle = new Style(typeof(Table));
                tabStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0,0,0,5)));
                tabStyle.Setters.Add(new Setter(Block.FontFamilyProperty, new FontFamily("Segoe UI")));
                tabStyle.Setters.Add(new Setter(Block.FontSizeProperty, 12d));
                document.Resources.Add(typeof(Table), tabStyle);
                fdr.Document = document;
            }
            return document;
        }

        public static string ByteArrayToHexString(byte[] Bytes)
        // FROM: http://stackoverflow.com/questions/623104/byte-to-hex-string/5919521#5919521
        {
            StringBuilder Result = new StringBuilder(Bytes.Length * 2);
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }

        public static ScrollViewer FindScroll(Visual visual)
        {
            if (visual is ScrollViewer)
                return visual as ScrollViewer;
            ScrollViewer searchChiled = null;
            DependencyObject chiled;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                chiled = VisualTreeHelper.GetChild(visual, i);
                if (chiled is Visual)
                    searchChiled = FindScroll(chiled as Visual);
                if (searchChiled != null)
                    return searchChiled;
            }
            return null;
        }

        // ===================================================================================
        // GUI EVENT HANDLERS - SERIAL STUFF
        // ===================================================================================

        private void but_Load_Click(object sender, RoutedEventArgs e)
        {
            String fileToRead = tb_FileName.Text;

            // Check if file exists
            if (File.Exists(fileToRead))
            {
                // Load data into memory
                byte[] fileBytes = File.ReadAllBytes(fileToRead);

                logWindow("Analyzing file...");

                // Add header to output
                updateTextBox(tb_Output, "--- FILE: " + fileToRead + "---", fontstyles.BOLD);
                updateTextBox(tb_RawOutput, "--- FILE: " + fileToRead + "---", fontstyles.BOLD);
                updateTextBox(tb_RawOutput, ""); // new place holder for appending the raw data to

                // Parse data and display
                // NOTE: We run that task in the background
                var bgWorker = new BackgroundWorker();
                bgWorker.DoWork += (s, ex) => {
                    // Do this work in the background...
                    rawDataDump(fileBytes);
                    parseAndShowData(fileBytes);
                };
                bgWorker.RunWorkerCompleted += (s, ex) => {
                    // Update the UI.
                    updateTextBox(tb_Output, "--- END OF FILE ---", fontstyles.BOLD);
                    updateTextBox(tb_RawOutput, "--- END OF FILE ---", fontstyles.BOLD);

                    logWindow("Finished analyzing file!");
                };
                bgWorker.RunWorkerAsync();
            }
            else
            {
                logWindow("ERROR: File does not exist!");
            }
        }

        private void but_Playback_Click(object sender, RoutedEventArgs e)
        {
            // Prompt for file to playback
            OpenFileDialog dlg_Image = new OpenFileDialog();
            dlg_Image.Title = "Open binary file to playback";
            dlg_Image.Filter = "All files (*.*)|*.*";
            bool? result = dlg_Image.ShowDialog(mainWindow);
            if ((bool)result)
            {
                String fileToPlayback = dlg_Image.FileName;

                // Serial Port already open?
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen == false)
                    {
                        // Open Serial Port using current settings
                        startSerialMonitoring();
                    }
                    // Successfully opened the port?
                    if (_serialPort.IsOpen)
                    {
                        byte[] data = File.ReadAllBytes(fileToPlayback);
                        logWindow("Sending data to serial...");

                        // Playback binary data
                        _serialPort.Write(data,0,data.Length);

                        logWindow("Finished playing back file...");
                    }
                }

            }



        }


        // ===================================================================================
        // FILE I/O STUFF
        // ===================================================================================

        private void raw_OpenFile()
        {
            try {
                // Open file so we can start writing away... 
                if (System.IO.Path.GetDirectoryName(outputFile).Length == 0)
                {
                    // no directory specified so write to current dir
                    outputFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), outputFile);
                }
                bw_output = new BinaryWriter(new FileStream(outputFile, outFileMode));
            }
            catch (Exception exc)
            {
                logWindow("Error: Cannot open output file for writing...");
                // Uncheck the box again
                cb_Output.IsChecked = false;
            }
        }

        private void raw_CloseFile()
        {
            // Stop file output
            if (bw_output != null)
            {
                bw_output.Close();
                bw_output = null;
            }


        }

        // ASSOCIATED UI HANDLERS
        private void tb_OutFileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            outputFile = tb_OutFileName.Text;
        }

        private void cb_OutputMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (optOverwrite.IsSelected)
            {
                outFileMode = FileMode.Create;
            }
            else
            {
                outFileMode = FileMode.Append;
            }
        }
        // ===================================================================================
        // GUI EVENT HANDLERS - MISC STUFF
        // ===================================================================================

        private void but_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg_Image = new OpenFileDialog();

            dlg_Image.Title = "Open binary file";
            dlg_Image.Filter = "All files (*.*)|*.*";
            bool? result = dlg_Image.ShowDialog(mainWindow);

            if ((bool)result)
            {
                tb_FileName.Text = dlg_Image.FileName;
            }
        }

        private void but_OutBrowse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg_OutFile = new SaveFileDialog();

            dlg_OutFile.Title = "Save binary file";
            dlg_OutFile.Filter = "All files (*.*)|*.*";
            dlg_OutFile.FileName = System.IO.Path.GetFileName(tb_OutFileName.Text);
            string initialDir = System.IO.Path.GetDirectoryName(tb_OutFileName.Text);
            if (initialDir.Length > 0)
            {
                dlg_OutFile.InitialDirectory = initialDir;
            } else
            {
                dlg_OutFile.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            }
            bool? result = dlg_OutFile.ShowDialog(mainWindow);

            if ((bool)result)
            {
                tb_OutFileName.Text = dlg_OutFile.FileName;
            }
        }

        private void cb_Output_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)cb_Output.IsChecked) {
                bRawDumpToFile = true;
                // Disable changing the filename of the output
                lab_OutFile.IsEnabled = false;
                tb_OutFileName.IsEnabled = false;
                but_OutBrowse.IsEnabled = false;
                cb_OutFileOptions.IsEnabled = false;
            }
            else
            {
                // Close the file if it is open... 
                bRawDumpToFile = false;
                raw_CloseFile();

                // Enable changing the filename of the output
                lab_OutFile.IsEnabled = true;
                tb_OutFileName.IsEnabled = true;
                but_OutBrowse.IsEnabled = true;
                cb_OutFileOptions.IsEnabled = true;

            }
        }

        private void cb_SerialAdvanced_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cb_SerialAdvanced.IsChecked)
            {
                // UI Enable
                lab_Baudrate.IsEnabled = true;
                tb_Baudrate.IsEnabled = true;
                lab_SerOptions.IsEnabled = true;
                cb_SerialOptions.IsEnabled = true;
                lab_SerDirection.IsEnabled = true;
                cb_SerialDirection.IsEnabled = true;
                cb_SerialDirection_SelectionChanged(null, null); // Update but_Playback according to actual settings
            }
            else
            {
                // UI Disable
                lab_Baudrate.IsEnabled = false;
                tb_Baudrate.IsEnabled = false;
                lab_SerOptions.IsEnabled = false;
                cb_SerialOptions.IsEnabled = false;
                lab_SerDirection.IsEnabled = false;
                cb_SerialDirection.IsEnabled = false;
                but_Playback.IsEnabled = false;
            }
        }
 
        private void rb_Checked(object sender, RoutedEventArgs e)
        // Because radio buttons are exclusive, we just use one routine for all radio buttons
        {
            if (rb_File.IsChecked == true)
            {
                // Show file "tab" in our hidden tabbar
                Dispatcher.BeginInvoke((Action)(() =>tc_Input.SelectedIndex = 0));
            }
            else if (rb_Serial.IsChecked == true)
            {
                // Rediscover serial ports
                populateSerialPorts();

                // Show serial tab in our hidden tabbar
                Dispatcher.BeginInvoke((Action)(() => tc_Input.SelectedIndex = 1));
            }

        }

        private void cb_SerialPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Already monitoring & connected?
            if (_serialPort.IsOpen)
            {
                // Stop current monitoring if ongoing
                stopSerialMonitoring();
                // Restart serial port with new options
                startSerialMonitoring();

            }
        }

        private void cb_SerialOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Already monitoring & connected?
            if (_serialPort.IsOpen)
            {
                // Stop current monitoring if ongoing
                stopSerialMonitoring();
                // Restart serial port with new options
                startSerialMonitoring();

            }
        }

        private void cb_SerialDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (optInput.IsSelected)
            {
                // Input mode
                if (but_Playback != null)       // Called during initialization too... 
                {
                    but_Playback.IsEnabled = false;
                }
            }
            else
            {
                // Output mode
                if (but_Playback != null)
                {
                    but_Playback.IsEnabled = true;
                }
            }
        }


        private void but_Start_Click(object sender, RoutedEventArgs e)
        {
            startSerialMonitoring();
        }

        private void but_Stop_Click(object sender, RoutedEventArgs e)
        {
            stopSerialMonitoring();
        }

 

        // ===================================================================================
        // HELPER FUNCTIONS
        // ===================================================================================

        public void logWindow(String logtext)
        {
            // Because this is possibly called through other threads, we need to handle this properly... 
            Dispatcher.BeginInvoke(new ThreadStart(delegate { sb_Label.Content = logtext; }));
        }

        // ===================================================================================
        // INTEGER TEXTBOX
        // ===================================================================================

        public static bool onlyNumeric(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that allows numeric input only
            return !regex.IsMatch(text); // 
        }

        private void tb_PreviewNumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !onlyNumeric(e.Text);
        }

        private void tb_PasteNumericOnly(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!onlyNumeric(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }


    }


}
