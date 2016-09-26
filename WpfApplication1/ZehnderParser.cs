using ComfoAirClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ComfoAirClient.ZehnderCommand;
using static ComfoAirClient.ZehnderData;

namespace ComfoAirClient
{
    class ZehnderParser
    {
        private ZehnderData cadata = new ZehnderData();
        public List<ZehnderCommand> parseResult;
        public int parseOffset = 0;

        public ZehnderParser()
        {
        }
        public ZehnderParser(byte[] input)
        {
            parseResult = ParseCommand(input);
        }

        // ===================================================================================
        // PARSE BYTE ARRAY and extract all known commands
        // ===================================================================================

        public List<ZehnderCommand> ParseCommand(byte[] input)
        {
            // Create new list to return
            List<ZehnderCommand> cmdList = new List<ZehnderCommand>();

            // Define some variables that we'll find useful
            int inputEnd = input.Length;
            int position = 0;

            // Loop through the entire dataset
            while (position < (inputEnd-2))
            {
                // Everything starts with a command initializer, so first we find that... 
                int cmdstart = findSequence(input, position, cadata.cst_StartCommand);
                int cmdstop = findSequence(input, position+2, cadata.cst_StopCommand);
                // Did we find a start and a stop of a command?
                // Note: we also check if the cmdstop is larger than the minimum possible command length!
                if( (cmdstart > 0) && (cmdstop >= (cmdstart + cadata.cst_MinCmdLength - 1)))
                {
                    // ----------------------------------------
                    // CREATE NEW ZehnderCommand AND FILL DATA
                    // ----------------------------------------
                    // New container for our parsed data
                    ZehnderCommand currentcmd = new ZehnderCommand();
                    // Checksum has offset 173 ... yes... don't ask me why... 
                    // Checksum = cmd + datalength + data
                    int checksum = 173;

                    // Read command structure and calculate checksum while we're at it
                    currentcmd.cmd_raw = new byte[2] { input[cmdstart + 2], input[cmdstart + 3] };
                    checksum += currentcmd.cmd_raw[0] + currentcmd.cmd_raw[1];
                    int datalength = input[cmdstart + 4];
                    checksum += datalength;
                    currentcmd.length = datalength;
                    currentcmd.data = new byte[datalength];
                    // Copy command data 
                    for (int i = 0; i < datalength; i++)
                    {
                        byte curbyte = input[cmdstart + 5 + i];
                        currentcmd.data[i] = curbyte;
                        checksum += (int)curbyte;
                    }
                    currentcmd.checksum = input[cmdstart + 5 + datalength];

                    // Did checksum work out?
                    if (currentcmd.checksum == (byte)checksum)
                    {
                        currentcmd.checksumOk = true;
                    }
                    // Look up command 


                    // ----------------------------------------
                    // ADD TO RESULT LIST
                    // ----------------------------------------
                    // Add command to result list
                    cmdList.Add(currentcmd);

                    // Adjust position to after this command
                    position = cmdstop;

                    // Retain end of currently parsed data
                    parseOffset = cmdstop + 2;
                }
                else
                {
                    // No command start or command stop not ok
                    break;
                }
            }
            parseResult = cmdList;
            return cmdList;
        }

        // ===================================================================================
        // PARSE DATA ARRAY and extract all known parameters
        // ===================================================================================

        public List<ZehnderParameter> ParseData(Command cmd, byte[] input)
        {
            // Create new list to return

            switch (cmd)
            {
                case Command.Req_ReadTemperatures:
                case Command.Res_ReadTemperatures:
                    return parsecmd_ReadTemperatures(input);
                case Command.Req_ReadTemperaturesExtended:
                case Command.Res_ReadTemperaturesExtended:
                    return parsecmd_ReadTemperaturesExtended(input);
                case Command.Req_ReadFanSteps:
                case Command.Res_ReadFanSteps:
                    return parsecmd_ReadFanSteps(input);
                default:
                    break;
            }

            // We should never get here -- return empty list if we do...
            return new List<ZehnderParameter>();
        }

        private List<ZehnderParameter> parsecmd_ReadFanSteps(byte[] data)
        {
            // Byte[1] - Abluft abwesend(%)
            // Byte[2] - Abluft niedrig / Stufe 1(%)
            // Byte[3] - Abluft mittel / Stufe 2(%)
            // Byte[4] - Zuluft Stufe abwesend (%)
            // Byte[5] - Zuluft niedrig / Stufe 1(%)
            // Byte[6] - Zuluft mittel / Stufe 2(%)
            // Byte[7] - Abluft aktuell(%)
            // Byte[8] - Zuluft aktuell(%)
            // Byte[9] - Aktuelle Stufe(Siehe Kommando 0x00 0x99)
            // Byte[10] - Zuluft Ventilator aktiv(1 = aktiv / 0 = inaktiv)
            // Byte[11] - Abluft hoch / Stufe 3(%)
            // Byte[12] - Zuluft hoch / Stufe 3(%)
            // Byte[13] -
            // Byte[14] -
            List <ZehnderParameter> paramList = new List<ZehnderParameter>();
            paramList.Add(new ZehnderParameter("Actual Mode", ((int)data[8]).ToString()));
            paramList.Add(new ZehnderParameter("Extraction Actual", ((int)data[6]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Pulsion Actual", ((int)data[7]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Extraction (away)", ((int)data[0]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Extraction (low/mode 1)", ((int)data[1]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Extraction (middle/mode 2)", ((int)data[2]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Extraction (high/mode 3)", ((int)data[10]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Pulsion (away)", ((int)data[3]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Pulsion (low/mode 1)", ((int)data[4]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Pulsion (middle/mode 2)", ((int)data[5]).ToString() + "%"));
            paramList.Add(new ZehnderParameter("Pulsion (high/mode 3)", ((int)data[11]).ToString() + "%"));
            if (data[9] == 0) {
                paramList.Add(new ZehnderParameter("Pulsion fan", "Inactive"));
            }
            else
            {
                paramList.Add(new ZehnderParameter("Pulsion fan", "Active"));
            }
            return paramList;
        }
        private List<ZehnderParameter> parsecmd_ReadTemperatures(byte[] data)
        {
            // Byte[1] - T1 / Außenluft (°C*)
            // Byte[2] - T2 / Zuluft(°C *)
            // Byte[3] - T3 / Abluft(°C *)
            // Byte[4] - T4 / Fortluft(°C *)
            List<ZehnderParameter> paramList = new List<ZehnderParameter>();
            paramList.Add(new ZehnderParameter("T1 - Outdoor Air",  ((int)data[0]).ToString() + " °C" ));
            paramList.Add(new ZehnderParameter("T2 - Pulsion", ((int)data[1]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T3 - Extraction", ((int)data[2]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T4 - Exhaust Air", ((int)data[3]).ToString() + " °C"));
            return paramList;
        }

        private List<ZehnderParameter> parsecmd_ReadTemperaturesExtended(byte[] data)
        {
            // Byte[1] - Comfort Temperature setting
            // Byte[2] - T1 / Außenluft (°C*)
            // Byte[3] - T2 / Zuluft(°C *)
            // Byte[4] - T3 / Abluft(°C *)
            // Byte[5] - T4 / Fortluft(°C *)
            // Byte[6] - Fühler anwesend: (1 = anwesend / 0 = abwesend)
            //  0x01 = T1 / Außenluft
            //  0x02 = T2 / Zuluft
            //  0x04 = T3 / Abluft
            //  0x08 = T4 / Fortluft
            //  0x10 = EWT
            //  0x20 = Nachheizung
            //  0x40 = Küchenhaube
            // Byte[7] - Temperatur EWT(°C *)
            // Byte[8] - Temperatur Nachheizung(°C *)
            // Byte[9] - Temperatur Küchenhaube(°C *)
            //
            // NOTE: (*) means we have a "zehnder" temperature (temperature+20)*2 ... 

            List<ZehnderParameter> paramList = new List<ZehnderParameter>();
            // First temperatures
            paramList.Add(new ZehnderParameter("Comfort Temperature", getTemperature(data[0]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T1 - Outdoor Air", getTemperature(data[1]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T2 - Pulsion", getTemperature(data[2]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T3 - Extraction", getTemperature(data[3]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("T4 - Exhaust Air", getTemperature(data[4]).ToString() + " °C"));
            // Present sensors:
            List<string> sensors = new List<string>();
            if ((data[5] & 0x01) > 0) { sensors.Add("T1"); };
            if ((data[5] & 0x02) > 0) { sensors.Add("T2"); };
            if ((data[5] & 0x04) > 0) { sensors.Add("T3"); };
            if ((data[5] & 0x08) > 0) { sensors.Add("T4"); };
            if ((data[5] & 0x10) > 0) { sensors.Add("Ground Coupled Heat Exchanger"); };
            if ((data[5] & 0x20) > 0) { sensors.Add("Post Heater"); };
            if ((data[5] & 0x40) > 0) { sensors.Add("Kitchen Hood"); };
            paramList.Add(new ZehnderParameter("Sensors present", string.Join(",", sensors.ToArray())));
            // Additional temperatures
            paramList.Add(new ZehnderParameter("Ground Heat Exchanger", getTemperature(data[6]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("Post Heater", getTemperature(data[7]).ToString() + " °C"));
            paramList.Add(new ZehnderParameter("Kitchen Hood", getTemperature(data[8]).ToString() + " °C"));
            return paramList;
        }


        // ===================================================================================
        // ZEHNDER CONVERSIONS
        // ===================================================================================

        private double getTemperature(byte zehnderTemp)
        {
            // Zehnder returns (TEMP + 20) * 2 so we need to convert back to regular temperatures;
            // We use floats to capture e.g. "20.5 degrees" - in principle we can measure that...  
            return (((double)zehnderTemp / 2) - 20);
        }

        // ===================================================================================
        // BYTE ARRAY OPERATION FILTERS
        // ===================================================================================

        private static int findSequence(byte[] array, int start, byte[] sequence)
        {
            int end = array.Length - sequence.Length; // past here no match is possible
            byte firstByte = sequence[0]; // cached to tell compiler there's no aliasing

            while (start < end)
            {
                // scan for first byte only. compiler-friendly.
                if (array[start] == firstByte)
                {
                    // scan for rest of sequence
                    for (int offset = 1; offset < sequence.Length; ++offset)
                    {
                        if (array[start + offset] != sequence[offset])
                        {
                            break; // mismatch? continue scanning with next byte
                        }
                        else if (offset == sequence.Length - 1)
                        {
                            return start; // all bytes matched!
                        }
                    }
                }
                ++start;
            }

            // end of array reached without match
            return -1;
        }

    }
}
