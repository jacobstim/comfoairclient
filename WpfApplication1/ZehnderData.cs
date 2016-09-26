using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComfoAirClient
{
    public sealed class ZehnderData
    {
        static readonly ZehnderData _instance = new ZehnderData();
        public static ZehnderData Instance
        {
            get
            {
                return _instance;
            }
        }

        // Minimum command length is 8: START (2b) + CMD (2b) + DATALENGTH (1b) + DATA (0-n) + CHECKSUM (1b) + STOP (2b)
        public readonly int cst_MinCmdLength = 8;


        public readonly byte[] cst_StartCommand = new byte[] { 0x07, 0xF0 };
        public readonly byte[] cst_StopCommand = new byte[] { 0x07, 0x0F };
        public readonly byte[] cst_AckCommand = new byte[] { 0x07, 0xF3 };


        public readonly byte[] cmdreq_BootLoaderVersion     = new byte[] { 0x00, 0x67 };
        public readonly byte[] cmdres_BootLoaderVersion     = new byte[] { 0x00, 0x68 };
        public readonly byte[] cmdreq_FirmwareVersion       = new byte[] { 0x00, 0x69 };
        public readonly byte[] cmdres_FirmwareVersion       = new byte[] { 0x00, 0x6A };
        public readonly byte[] cmdreq_ConnectorBoardVersion = new byte[] { 0x00, 0xA1 };
        public readonly byte[] cmdres_ConnectorBoardVersion = new byte[] { 0x00, 0xA2 };

        public readonly byte[] cmdreq_ReadInputs            = new byte[] { 0x00, 0x03 };
        public readonly byte[] cmdres_ReadInputs            = new byte[] { 0x00, 0x04 };
        public readonly byte[] cmdreq_ReadFan               = new byte[] { 0x00, 0x0B };
        public readonly byte[] cmdres_ReadFan               = new byte[] { 0x00, 0x0C };
        public readonly byte[] cmdreq_ReadFlaps             = new byte[] { 0x00, 0x0D };
        public readonly byte[] cmdres_ReadFlaps             = new byte[] { 0x00, 0x0E };
        public readonly byte[] cmdreq_ReadTemperature       = new byte[] { 0x00, 0x0F };
        public readonly byte[] cmdres_ReadTemperature       = new byte[] { 0x00, 0x10 };
        public readonly byte[] cmdreq_ReadButtons           = new byte[] { 0x00, 0x11 };
        public readonly byte[] cmdres_ReadButtons           = new byte[] { 0x00, 0x12 };
        public readonly byte[] cmdreq_ReadAnalogs           = new byte[] { 0x00, 0x13 };
        public readonly byte[] cmdres_ReadAnalogs           = new byte[] { 0x00, 0x14 };
        public readonly byte[] cmdreq_ReadSensors           = new byte[] { 0x00, 0x97 };
        public readonly byte[] cmdres_ReadSensors           = new byte[] { 0x00, 0x98 };
        public readonly byte[] cmdreq_ReadRS232Mode         = new byte[] { 0x00, 0x9B };
        public readonly byte[] cmdres_ReadRS232Mode         = new byte[] { 0x00, 0x9C };
        public readonly byte[] cmdreq_ReadAnalogValues      = new byte[] { 0x00, 0x9D };
        public readonly byte[] cmdres_ReadAnalogValues      = new byte[] { 0x00, 0x9E };

        public enum Command
        {
            Unknown,
            Req_ReadInputs, Res_ReadInputs,
            Req_ReadFans, Res_ReadFans,
            Req_ReadFlaps, Res_ReadFlaps,
            Req_ReadTemperatures, Res_ReadTemperatures,
            Req_ReadButtons, Res_ReadButtons,
            Req_ReadAnalogs, Res_ReadAnalogs,
            Req_BootLoaderVersion, Res_BootLoaderVersion,
            Req_FirmwareVersion, Res_FirmwareVersion,
            Req_ReadSensors, Res_ReadSensors,
            Req_RS232Mode, Res_RS232Mode,
            Req_WriteFanSteps,
            Req_ReadAnalogValues, Res_ReadAnalogValues, Req_WriteAnalogValues,
            Req_ConnectorBoardVersion, Res_ConnectorBoardVersion,
            Req_ReadDelays, Res_ReadDelays, Req_WriteDelays,
            Req_ReadFanSteps, Res_ReadFanSteps, Req_WriteFanStepsExtended,
            Req_ReadTemperaturesExtended, Res_ReadTemperaturesExtended, Req_WriteTemperaturesExtended,
            Req_ReadUnitStatus, Res_ReadUnitStatus, Req_WriteUnitStatus,
            Req_ReadUnitErrors, Res_ReadUnitErrors, Req_WriteUnitErrors,
            Req_ReadDurations, Res_ReadDurations,
            Req_ReadBypassStatus, Res_ReadBypassStatus,
            Req_ReadPreheaterStatus, Res_ReadPreheaterStatus, Req_WritePreheaterStatus,
            Req_ReadRFStatus, Res_ReadRFStatus, Req_SetRFAddress,
            Req_ReadPreheatingTemperatures, Res_ReadPreheatingTemperatures,
            Req_ReadPostheaterStatus, Res_ReadPostheaterStatus,

            Req_CCEaseParameters, Res_CCEaseParameters
        }


        // Command to String mappings
        public static Dictionary<Command, string> cmd_Strings = new Dictionary<Command, string>
        {
            {Command.Unknown, "N/A" },
            {Command.Req_ReadInputs, "REQ_ReadInputs"}, {Command.Res_ReadInputs, "RES_ReadInputs"},
            {Command.Req_ReadFans, "REQ_ReadFans"}, {Command.Res_ReadFans, "RES_ReadFans"},
            {Command.Req_ReadFlaps, "REQ_ReadFlaps"}, {Command.Res_ReadFlaps, "RES_ReadFlaps"},
            {Command.Req_ReadTemperatures, "REQ_ReadTemperatures"}, {Command.Res_ReadTemperatures, "RES_ReadTemperatures"},
            {Command.Req_ReadButtons, "REQ_ReadButtons"}, {Command.Res_ReadButtons, "RES_ReadButtons"},
            {Command.Req_ReadAnalogs, "REQ_ReadAnalogs"}, {Command.Res_ReadAnalogs, "RES_ReadAnalogs"},

            {Command.Req_BootLoaderVersion, "REQ_BootloaderVersion"}, {Command.Res_BootLoaderVersion, "RES_BootloaderVersion"},
            {Command.Req_FirmwareVersion, "REQ_FirmwareVersion"}, {Command.Res_FirmwareVersion, "RES_FirmwareVersion"},
            {Command.Req_ReadSensors, "REQ_ReadSensors" },{Command.Res_ReadSensors,"RES_ReadSensors" },
            {Command.Req_RS232Mode, "REQ_RS232Mode" },{Command.Res_RS232Mode,"RES_RS232Mode" },
            {Command.Req_WriteFanSteps,"REQ_WriteFanSteps" },
            {Command.Req_ReadAnalogValues, "REQ_ReadAnalogValues" },{Command.Res_ReadAnalogValues, "RES_ReadAnalogValues" },{Command.Req_WriteAnalogValues,"REQ_WriteAnalogValues" },
            {Command.Req_ConnectorBoardVersion, "REQ_ConnectorBoardVersion"}, {Command.Res_ConnectorBoardVersion, "RES_ConnectorBoardVersion"},
            {Command.Req_ReadDelays,"REQ_ReadDelays" },{Command. Res_ReadDelays,"RES_ReadDelays" },{Command.Req_WriteDelays,"REQ_WriteDelays" },
            {Command.Req_ReadFanSteps, "REQ_ReadFanSteps" },{Command.Res_ReadFanSteps, "RES_ReadFanSteps" },{Command.Req_WriteFanStepsExtended,"REQ_WriteFanStepsExtended" },
            {Command.Req_ReadTemperaturesExtended, "REQ_ReadTemperaturesExtended" },{Command.Res_ReadTemperaturesExtended, "RES_ReadTemperaturesExtended" },{Command.Req_WriteTemperaturesExtended,"REQ_WriteTemperaturesExtended" },
            {Command.Req_ReadUnitStatus, "REQ_ReadUnitStatus" },{Command.Res_ReadUnitStatus, "RES_ReadUnitStatus" },{Command.Req_WriteUnitStatus,"REQ_WriteUnitStatus" },
            {Command.Req_ReadUnitErrors, "REQ_ReadUnitErrors" },{Command.Res_ReadUnitErrors, "RES_ReadUnitErrors" },{Command.Req_WriteUnitErrors,"REQ_WriteUnitErrors" },
            {Command.Req_ReadDurations, "REQ_ReadDurations" },{Command.Res_ReadDurations,"RES_ReadDurations" },
            {Command.Req_ReadBypassStatus, "REQ_ReadBypassStatus" },{Command.Res_ReadBypassStatus,"RES_ReadBypassStatus" },
            {Command.Req_ReadPreheaterStatus, "REQ_ReadPreheaterStatus" },{Command.Res_ReadPreheaterStatus, "RES_ReadPreheaterStatus" },{Command.Req_WritePreheaterStatus,"REQ_WritePreheaterStatus" },
            {Command.Req_ReadRFStatus, "REQ_ReadRFStatus" },{Command.Res_ReadRFStatus,"RES_ReadRFStatus" }, {Command.Req_SetRFAddress,"REQ_SetRFAddress" },
            {Command.Req_ReadPreheatingTemperatures, "REQ_ReadPreheatingTemperatures" },{Command.Res_ReadPreheatingTemperatures,"RES_ReadPreheatingTemperatures" },
            {Command.Req_ReadPostheaterStatus, "REQ_ReadPostheaterStatus" },{Command.Res_ReadPostheaterStatus,"RES_ReadPostheaterStatus" },

            {Command.Req_CCEaseParameters, "REQ_CCEaseParameters" },{Command.Res_CCEaseParameters,"RES_CCEaseParameters" }

        };


        // The long list of INDEX -> COMMAND mappings
        public readonly Command[] cmd_Commands = new Command[]
        {
            Command.Unknown, // 0x00
            Command.Unknown, // 0x01
            Command.Unknown, // 0x02
            Command.Req_ReadInputs, // 0x03
            Command.Res_ReadInputs, // 0x04
            Command.Unknown, // 0x05
            Command.Unknown, // 0x06
            Command.Unknown, // 0x07
            Command.Unknown, // 0x08
            Command.Unknown, // 0x09
            Command.Unknown, // 0x0A
            Command.Req_ReadFans, // 0x0B
            Command.Res_ReadFans, // 0x0C
            Command.Req_ReadFlaps, // 0x0D
            Command.Res_ReadFlaps, // 0x0E
            Command.Req_ReadTemperatures, // 0x0F
            Command.Res_ReadTemperatures, // 0x10
            Command.Req_ReadButtons, // 0x11
            Command.Res_ReadButtons, // 0x12
            Command.Req_ReadAnalogs, // 0x13
            Command.Res_ReadAnalogs, // 0x14
            Command.Unknown, // 0x15
            Command.Unknown, // 0x16
            Command.Unknown, // 0x17
            Command.Unknown, // 0x18
            Command.Unknown, // 0x19
            Command.Unknown, // 0x1A
            Command.Unknown, // 0x1B
            Command.Unknown, // 0x1C
            Command.Unknown, // 0x1D
            Command.Unknown, // 0x1E
            Command.Unknown, // 0x1F
            Command.Unknown, // 0x20
            Command.Unknown, // 0x21
            Command.Unknown, // 0x22
            Command.Unknown, // 0x23
            Command.Unknown, // 0x24
            Command.Unknown, // 0x25
            Command.Unknown, // 0x26
            Command.Unknown, // 0x27
            Command.Unknown, // 0x28
            Command.Unknown, // 0x29
            Command.Unknown, // 0x2A
            Command.Unknown, // 0x2B
            Command.Unknown, // 0x2C
            Command.Unknown, // 0x2D
            Command.Unknown, // 0x2E
            Command.Unknown, // 0x2F
            Command.Unknown, // 0x30
            Command.Unknown, // 0x31
            Command.Unknown, // 0x32
            Command.Unknown, // 0x33
            Command.Unknown, // 0x34
            Command.Req_CCEaseParameters, // 0x35
            Command.Unknown, // 0x36
            Command.Unknown, // 0x37
            Command.Unknown, // 0x38
            Command.Unknown, // 0x39
            Command.Unknown, // 0x3A
            Command.Unknown, // 0x3B
            Command.Res_CCEaseParameters, // 0x3C
            Command.Unknown, // 0x3D
            Command.Req_SetRFAddress, // 0x3E
            Command.Unknown, // 0x3F
            Command.Unknown, // 0x40
            Command.Unknown, // 0x41
            Command.Unknown, // 0x42
            Command.Unknown, // 0x43
            Command.Unknown, // 0x44
            Command.Unknown, // 0x45
            Command.Unknown, // 0x46
            Command.Unknown, // 0x47
            Command.Unknown, // 0x48
            Command.Unknown, // 0x49
            Command.Unknown, // 0x4A
            Command.Unknown, // 0x4B
            Command.Unknown, // 0x4C
            Command.Unknown, // 0x4D
            Command.Unknown, // 0x4E
            Command.Unknown, // 0x4F
            Command.Unknown, // 0x50
            Command.Unknown, // 0x51
            Command.Unknown, // 0x52
            Command.Unknown, // 0x53
            Command.Unknown, // 0x54
            Command.Unknown, // 0x55
            Command.Unknown, // 0x56
            Command.Unknown, // 0x57
            Command.Unknown, // 0x58
            Command.Unknown, // 0x59
            Command.Unknown, // 0x5A
            Command.Unknown, // 0x5B
            Command.Unknown, // 0x5C
            Command.Unknown, // 0x5D
            Command.Unknown, // 0x5E
            Command.Unknown, // 0x5F
            Command.Unknown, // 0x60
            Command.Unknown, // 0x61
            Command.Unknown, // 0x62
            Command.Unknown, // 0x63
            Command.Unknown, // 0x64
            Command.Unknown, // 0x65
            Command.Unknown, // 0x66
            Command.Req_BootLoaderVersion, // 0x67
            Command.Res_BootLoaderVersion, // 0x68
            Command.Req_FirmwareVersion, // 0x69
            Command.Res_FirmwareVersion, // 0x6A
            Command.Unknown, // 0x6B
            Command.Unknown, // 0x6C
            Command.Unknown, // 0x6D
            Command.Unknown, // 0x6E
            Command.Unknown, // 0x6F
            Command.Unknown, // 0x70
            Command.Unknown, // 0x71
            Command.Unknown, // 0x72
            Command.Unknown, // 0x73
            Command.Unknown, // 0x74
            Command.Unknown, // 0x75
            Command.Unknown, // 0x76
            Command.Unknown, // 0x77
            Command.Unknown, // 0x78
            Command.Unknown, // 0x79
            Command.Unknown, // 0x7A
            Command.Unknown, // 0x7B
            Command.Unknown, // 0x7C
            Command.Unknown, // 0x7D
            Command.Unknown, // 0x7E
            Command.Unknown, // 0x7F
            Command.Unknown, // 0x80
            Command.Unknown, // 0x81
            Command.Unknown, // 0x82
            Command.Unknown, // 0x83
            Command.Unknown, // 0x84
            Command.Unknown, // 0x85
            Command.Unknown, // 0x86
            Command.Unknown, // 0x87
            Command.Unknown, // 0x88
            Command.Unknown, // 0x89
            Command.Unknown, // 0x8A
            Command.Unknown, // 0x8B
            Command.Unknown, // 0x8C
            Command.Unknown, // 0x8D
            Command.Unknown, // 0x8E
            Command.Unknown, // 0x8F
            Command.Unknown, // 0x90
            Command.Unknown, // 0x91
            Command.Unknown, // 0x92
            Command.Unknown, // 0x93
            Command.Unknown, // 0x94
            Command.Unknown, // 0x95
            Command.Unknown, // 0x96
            Command.Req_ReadSensors, // 0x97
            Command.Res_ReadSensors, // 0x98
            Command.Req_WriteFanSteps, // 0x99
            Command.Unknown, // 0x9A
            Command.Req_RS232Mode, // 0x9B
            Command.Res_RS232Mode, // 0x9C
            Command.Req_ReadAnalogValues, // 0x9D
            Command.Res_ReadAnalogValues, // 0x9E
            Command.Req_WriteAnalogValues, // 0x9F
            Command.Unknown, // 0xA0
            Command.Req_ConnectorBoardVersion, // 0xA1
            Command.Res_ConnectorBoardVersion, // 0xA2
            Command.Unknown, // 0xA3
            Command.Unknown, // 0xA4
            Command.Unknown, // 0xA5
            Command.Unknown, // 0xA6
            Command.Unknown, // 0xA7
            Command.Unknown, // 0xA8
            Command.Unknown, // 0xA9
            Command.Unknown, // 0xAA
            Command.Unknown, // 0xAB
            Command.Unknown, // 0xAC
            Command.Unknown, // 0xAD
            Command.Unknown, // 0xAE
            Command.Unknown, // 0xAF
            Command.Unknown, // 0xB0
            Command.Unknown, // 0xB1
            Command.Unknown, // 0xB2
            Command.Unknown, // 0xB3
            Command.Unknown, // 0xB4
            Command.Unknown, // 0xB5
            Command.Unknown, // 0xB6
            Command.Unknown, // 0xB7
            Command.Unknown, // 0xB8
            Command.Unknown, // 0xB9
            Command.Unknown, // 0xBA
            Command.Unknown, // 0xBB
            Command.Unknown, // 0xBC
            Command.Unknown, // 0xBD
            Command.Unknown, // 0xBE
            Command.Unknown, // 0xBF
            Command.Unknown, // 0xC0
            Command.Unknown, // 0xC1
            Command.Unknown, // 0xC2
            Command.Unknown, // 0xC3
            Command.Unknown, // 0xC4
            Command.Unknown, // 0xC5
            Command.Unknown, // 0xC6
            Command.Unknown, // 0xC7
            Command.Unknown, // 0xC8
            Command.Req_ReadDelays, // 0xC9
            Command.Res_ReadDelays, // 0xCA
            Command.Req_WriteDelays, // 0xCB
            Command.Unknown, // 0xCC
            Command.Req_ReadFanSteps, // 0xCD
            Command.Res_ReadFanSteps, // 0xCE
            Command.Req_WriteFanStepsExtended, // 0xCF
            Command.Unknown, // 0xD0
            Command.Req_ReadTemperaturesExtended, // 0xD1
            Command.Res_ReadTemperaturesExtended, // 0xD2
            Command.Req_WriteTemperaturesExtended, // 0xD3
            Command.Unknown, // 0xD4
            Command.Req_ReadUnitStatus, // 0xD5
            Command.Res_ReadUnitStatus, // 0xD6
            Command.Req_WriteUnitStatus, // 0xD7
            Command.Unknown, // 0xD8
            Command.Req_ReadUnitErrors, // 0xD9
            Command.Res_ReadUnitErrors, // 0xDA
            Command.Req_WriteUnitErrors, // 0xDB
            Command.Unknown, // 0xDC
            Command.Req_ReadDurations, // 0xDD
            Command.Res_ReadDurations, // 0xDE
            Command.Req_ReadBypassStatus, // 0xDF
            Command.Res_ReadBypassStatus, // 0xE0
            Command.Req_ReadPreheaterStatus, // 0xE1
            Command.Res_ReadPreheaterStatus, // 0xE2
            Command.Req_WritePreheaterStatus, // 0xE3
            Command.Unknown, // 0xE4
            Command.Req_ReadRFStatus, // 0xE5
            Command.Res_ReadRFStatus, // 0xE6
            Command.Unknown, // 0xE7
            Command.Unknown, // 0xE8
            Command.Req_ReadPreheatingTemperatures, // 0xE9
            Command.Res_ReadPreheatingTemperatures, // 0xEA
            Command.Req_ReadPostheaterStatus, // 0xEB
            Command.Res_ReadPostheaterStatus, // 0xEC
            Command.Unknown, // 0xED
            Command.Unknown, // 0xEE
            Command.Unknown, // 0xEF
            Command.Unknown, // 0xF0
            Command.Unknown, // 0xF1
            Command.Unknown, // 0xF2
            Command.Unknown, // 0xF3
            Command.Unknown, // 0xF4
            Command.Unknown, // 0xF5
            Command.Unknown, // 0xF6
            Command.Unknown, // 0xF7
            Command.Unknown, // 0xF8
            Command.Unknown, // 0xF9
            Command.Unknown, // 0xFA
            Command.Unknown, // 0xFB
            Command.Unknown, // 0xFC
            Command.Unknown, // 0xFD
            Command.Unknown, // 0xFE
            Command.Unknown // 0xFF
        };


    }
}
