using ComfoAirClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
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
        }

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
                cb_SerialPort.Items.Add(sPort);
            }
            // Did we get items?
            if (cb_SerialPort.Items.Count == 0)
            {
                cb_SerialPort.Items.Add("No serial devices found!");
                cb_SerialPort.SelectedIndex = 0;
                cb_SerialPort.IsEnabled = false;
                but_Start.IsEnabled = false;
                but_Stop.IsEnabled = false;
            } else
            {
                cb_SerialPort.SelectedIndex = 0;
                cb_SerialPort.IsEnabled = true;
                but_Start.IsEnabled = true;
                but_Stop.IsEnabled = false;
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
            _serialPort.BaudRate = 9600;
            _serialPort.Handshake = Handshake.None;
            _serialPort.PortName = cb_SerialPort.Text;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);

            // Start output
            updateTextBox(tb_Output, "--- SERIAL PORT: " + _serialPort.PortName + " (" + serOptions + ")---\n", fontstyles.BOLD);
            updateTextBox(tb_RawOutput, "\n--- SERIAL PORT: " + _serialPort.PortName + " (" + serOptions + ")---\n", fontstyles.BOLD);

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

                // Prepare for parsing; first check if there is still outstanding data to parse...
                byte[] truncBuffer = new byte[bytesRead + processSize];
                if (processSize > 0)
                {
                    // Copy outstanding data to new buffer
                    Array.Copy(processBuffer, 0, truncBuffer, 0, processSize);
                    // Copy from our serial buffer the actual length read to avoid a bunch of redundant zeroes when parsing... 
                    Array.Copy(serialBuffer, 0, truncBuffer, processSize, bytesRead);
                    // Clear "cue" of waiting data... 
                    dropProcessBuffer();
                }
                else
                {
                    // Just copy directly to our truncbuffer
                    Array.Copy(serialBuffer, 0, truncBuffer, 0, bytesRead);
                }

                // Feed data to parser
                int processedOffset = parseAndShowData(truncBuffer);

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
            // Append data to the raw output
            //Dispatcher.BeginInvoke(new ThreadStart(delegate { tb_RawOutput.Text += ByteArrayToHexString(inputBytes); }));
            updateTextBox(tb_RawOutput, ByteArrayToHexString(inputBytes));

            // Parse the file into Zehnder commands
            ZehnderParser zParser = new ZehnderParser(inputBytes);
            List<ZehnderCommand> cmds = zParser.parseResult;

            // Output all data found
            foreach (ZehnderCommand cmd in cmds)
            {
                //updateTextBox(tb_Output, cmd.ToString());
                addToTextBox(tb_Output, cmd.ToTable());
                addToTextBox(tb_Output, new BlockUIContainer(new Separator()));
            }
            // Return last parsed position in the supplied buffer
            return zParser.parseOffset;

        }

        // ===================================================================================
        // DATA VISUALISATION CODE
        // ===================================================================================

        private void updateTextBox(TextBox tb, string text)
        // Update a regular TextBox
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate {
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

        private void addToTextBox(FlowDocumentReader fdr, Block TextBlock)
        {
            // Check if document exists in FlowDocumentReader, otherwise create one
            FlowDocument document = checkDocument(fdr);
            document.Blocks.Add(TextBlock);
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

        // ===================================================================================
        // GUI EVENT HANDLERS
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

        private void but_Load_Click(object sender, RoutedEventArgs e)
        {
            String fileToRead = tb_FileName.Text;

            // Check if file exists
            if (File.Exists(fileToRead)) {
                // Load data into memory
                byte[] fileBytes = File.ReadAllBytes(fileToRead);

                // Add header to output
                updateTextBox(tb_Output, "--- FILE: " + fileToRead + "---", fontstyles.BOLD);
                updateTextBox(tb_RawOutput, "--- FILE: " + fileToRead + "---", fontstyles.BOLD);

                // Parse data and display
                parseAndShowData(fileBytes);
                updateTextBox(tb_Output, "--- END OF FILE ---", fontstyles.BOLD);
                updateTextBox(tb_RawOutput, "--- END OF FILE ---", fontstyles.BOLD);

            }
            else
            {
                logWindow("ERROR: File does not exist!");
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

    }
}
