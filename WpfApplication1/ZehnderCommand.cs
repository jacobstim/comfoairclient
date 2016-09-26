using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using static ComfoAirClient.ZehnderData;

namespace ComfoAirClient
{
    class ZehnderCommand
    {

        public byte[] cmd_raw { get; set; }                 // Raw command bytes
        public int length { get; set; }                     // Binary data length
        public byte[] data { get; set; }                    // Actual binary data
        public byte checksum { get; set; }                  // Calculated checksum
        public bool checksumOk { get; set; }                // Whether the checksum is ok or not
        public bool ackReceived { get; set; }               // Ack received for this command?

        //public ZehnderResponse response { get; set; }       // Response received for the command; 

        public override String ToString()
        {
            return "CMD: " + CmdString() + " | DATA: " + RawDataString();
        }

        // Returns the entire command as a string
        public String RawDataString()
        {
            return byteArrayToString(data);
        }

        public Command getCommand()
        {
            int cmdindex = byteArrayToInt(cmd_raw);
            Command cmd = Command.Unknown;
            if (cmdindex < ZehnderData.Instance.cmd_Commands.Length)
            {
                cmd = ZehnderData.Instance.cmd_Commands[cmdindex];
            }
            return cmd;
        }
        public String CmdString()
        {
            Command cmd = getCommand();
            return ZehnderData.cmd_Strings[cmd] + " (" + byteArrayToString(cmd_raw) + ")";
        }

        // Convert to a Document Table for easy displaying in a decent way... 
        public Table ToTable()
        {
            Table newTable = new Table();
            // Create two columns
            newTable.Columns.Add(new TableColumn());        // Item name
            newTable.Columns.Add(new TableColumn());        // Item value
            GridLengthConverter glc = new GridLengthConverter();
            newTable.Columns[0].Width = (GridLength)glc.ConvertFromString("100"); //new GridLength(0, GridUnitType.Auto);
            // Create a RowGroup to hold the data
            newTable.RowGroups.Add(new TableRowGroup());

            // First rows is the command
            newTable.RowGroups[0].Rows.Add(new TableRow());
            TableRow currentRow = newTable.RowGroups[0].Rows[0];
            currentRow.Cells.Add(new TableCell(new Paragraph(new Run("Command"))));
            currentRow.Cells.Add(new TableCell(new Paragraph(new Run(CmdString()))));
            currentRow.Cells[0].FontWeight = FontWeights.Bold;

            // Second row is the data
            newTable.RowGroups[0].Rows.Add(new TableRow());
            currentRow = newTable.RowGroups[0].Rows[1];
            currentRow.Cells.Add(new TableCell(new Paragraph(new Run("Data"))));
            currentRow.Cells.Add(new TableCell());
            currentRow.Cells[0].FontWeight = FontWeights.Bold;

            // Parse all data in a new table (inline to the 2nd cell for the data row)
            List<ZehnderParameter> zParams = (new ZehnderParser()).ParseData(getCommand(), data);
            if (zParams.Count > 0)
            {
                Table dataTable = new Table();
                dataTable.Columns.Add(new TableColumn());        // Parameter name
                dataTable.Columns.Add(new TableColumn());        // Parameter value
                dataTable.Columns[0].Width = (GridLength)glc.ConvertFromString("150");
                dataTable.RowGroups.Add(new TableRowGroup());

                // Convert array into a new table
                int counter = 0;
                foreach (ZehnderParameter zParam in zParams)
                {
                    // Add a new row per parameter in the data
                    dataTable.RowGroups[0].Rows.Add(new TableRow());
                    TableRow currentDataRow = dataTable.RowGroups[0].Rows[counter];
                    // Add cells with content to the third row.
                    currentDataRow.Cells.Add(new TableCell(new Paragraph(new Run(zParam.ParamName))));
                    currentDataRow.Cells.Add(new TableCell(new Paragraph(new Run(zParam.ParamValue))));
                    //currentDataRow.Cells[0].FontWeight = FontWeights.Bold;
                    currentDataRow.Cells[0].TextAlignment = TextAlignment.Left;
                    counter++;
                }

                // Add to data cell in main table
                currentRow.Cells[1].Blocks.Add(dataTable);
            }
            // Add raw command data to the bottom
            currentRow.Cells[1].Blocks.Add(new Paragraph(new Run("(" + RawDataString() + ")")));
            return newTable;
        }

        private String byteArrayToString(byte[] input)
        {
            // Convert entire byte array to hex output
            return "0x" + string.Concat(input.Select(b => b.ToString("X2")));
        }

        private int byteArrayToInt(byte[] input)
        {
            int resultint = 0;
            // Convert entire byte array to int (so max length is 4 bytes...)
            for (int i = 0; i < input.Length; i++)
            {
                resultint += ((int)input[input.Length - 1 - i]) << (8*i);
            }
            return resultint;
        }

    }

    //class ZehnderResponse
    //{
    //    public ZehnderCommand.Command cmd { get; set; }     // Command replying to
    //    public byte[] cmd_raw { get; set; }                 // Raw command bytes
    //    public int length { get; set; }                     // Binary data length
    //    public byte[] data { get; set; }                    // Actual binary data
    //    public byte checksum { get; set; }                  // Calculated checksum
    //}

    class ZehnderParameter
    {
        public ZehnderParameter(String pName, String pValue)
        {
            ParamName = pName;
            ParamValue = pValue;
        }

        public String ParamName { get; set; }
        public String ParamValue { get; set; }
    }

}
