﻿using Core;
using NScci;
using System;

namespace mml2vgmIDE
{
    public class dacControl
    {

        private const byte DCTRL_LMODE_IGNORE = 0x00;
        private const byte DCTRL_LMODE_CMDS = 0x01;
        private const byte DCTRL_LMODE_MSEC = 0x02;
        private const byte DCTRL_LMODE_TOEND = 0x03;
        public const byte DCTRL_LMODE_BYTES = 0x0F;

        private const int MAX_CHIPS = 0xFF;
        private dac_control[] DACData = new dac_control[MAX_CHIPS];
        public ChipRegister chipRegister = null;
        public EnmModel model = EnmModel.VirtualModel;


        public void sendCommand(long Counter,dac_control chip)
        {
            byte Port;
            byte Command;
            byte Data;
            outDatum od;

            if ((chip.Running & 0x10) > 0)   // command already sent
                return;
            if (chip.DataStart + chip.RealPos >= chip.DataLen)
                return;

            //if (! chip->Reverse)
            //ChipData00 = chip.Data[(chip.DataStart + chip.RealPos)];
            //ChipData01 = chip.Data[(chip.DataStart + chip.RealPos+1)];
            //else
            //	ChipData = chip->Data + (chip->DataStart + chip->CmdsToSend - 1 - chip->Pos);
            switch (chip.DstChipType)
            {
                // Support for the important chips
                case 0x02:  // YM2612 (16-bit Register (actually 9 Bit), 8-bit Data)
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[(chip.DataStart + chip.RealPos)];
                    od = chip.od;
                    //if (model == enmModel.RealModel) log.Write(string.Format("{0:x} {1:x}", Data, chip.RealPos));

                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, Port, Command, Data, od);
                    break;
                case 0x11:  // PWM (4-bit Register, 12-bit Data)
                    Port = (byte)((chip.DstCommand & 0x000F) >> 0);
                    Command = (byte)(chip.Data[chip.DataStart + chip.RealPos + 1] & 0x0F);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    od = chip.od;
                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, Port, Command, Data,od);
                    break;
                // Support for other chips (mainly for completeness)
                case 0x00:  // SN76496 (4-bit Register, 4-bit/10-bit Data)
                    Command = (byte)((chip.DstCommand & 0x00F0) >> 0);
                    Data = (byte)(chip.Data[chip.DataStart + chip.RealPos] & 0x0F);
                    od = chip.od;
                    if ((Command & 0x10) > 0)
                    {
                        // Volume Change (4-Bit value)
                        chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, 0x00, (byte)(Command | Data),od);
                    }
                    else
                    {
                        // Frequency Write (10-Bit value)
                        Port = (byte)(((chip.Data[chip.DataStart + chip.RealPos + 1] & 0x03) << 4) | ((chip.Data[chip.DataStart + chip.RealPos] & 0xF0) >> 4));
                        chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, 0x00, (byte)(Command | Data),od);
                        chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, 0x00, Port,od);
                    }
                    break;
                case 0x18:  // OKIM6295 - TODO: verify
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    od = chip.od;

                    if (Command == 0)
                    {
                        Port = (byte)((chip.DstCommand & 0x0F00) >> 8);
                        if ((Data & 0x80) > 0)
                        {
                            // Sample Start
                            // write sample ID
                            chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, Command, Data,od);
                            // write channel(s) that should play the sample
                            chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, Command, (byte)(Port << 4),od);
                        }
                        else
                        {
                            // Sample Stop
                            chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, Command, (byte)(Port << 3),od);
                        }
                    }
                    else
                    {
                        chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, Command, Data,od);
                    }
                    break;
                // Generic support: 8-bit Register, 8-bit Data
                case 0x01:  // YM2413
                case 0x03:  // YM2151
                case 0x06:  // YM2203
                case 0x09:  // YM3812
                case 0x0A:  // YM3526
                case 0x0B:  // Y8950
                case 0x0F:  // YMZ280B
                case 0x12:  // AY8910
                case 0x13:  // GameBoy DMG
                case 0x14:  // NES APU
                            //	case 0x15:	// MultiPCM
                case 0x16:  // UPD7759
                case 0x17:  // OKIM6258
                case 0x1D:  // K053260 - TODO: Verify
                case 0x1E:  // Pokey - TODO: Verify
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    od = chip.od;
                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, Command, Data,od);
                    break;
                // Generic support: 16-bit Register, 8-bit Data
                case 0x07:  // YM2608
                case 0x08:  // YM2610/B
                case 0x0C:  // YMF262
                case 0x0D:  // YMF278B
                case 0x0E:  // YMF271
                case 0x19:  // K051649 - TODO: Verify
                case 0x1A:  // K054539 - TODO: Verify
                case 0x1C:  // C140 - TODO: Verify
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    od = chip.od;
                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, Port, Command, Data,od);
                    break;
                // Generic support: 8-bit Register with Channel Select, 8-bit Data
                case 0x05:  // RF5C68
                case 0x10:  // RF5C164
                case 0x1B:  // HuC6280
                    Port = (byte)((chip.DstCommand & 0xFF00) >> 8);
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    Data = chip.Data[chip.DataStart + chip.RealPos];
                    od = chip.od;

                    if (Port != 0xFF)   // Send Channel Select
                        chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, (byte)(Command >> 4), Port,od);
                    // Send Data
                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, 0x00, (byte)(Command & 0x0F), Data,od);
                    break;
                // Generic support: 8-bit Register, 16-bit Data
                case 0x1F:  // QSound
                    Command = (byte)((chip.DstCommand & 0x00FF) >> 0);
                    od = chip.od;
                    chip_reg_write(Counter, chip.DstChipType, chip.DstChipID, chip.Data[chip.DataStart + chip.RealPos], chip.Data[chip.DataStart + chip.RealPos + 1], Command,od);
                    break;
            }
            chip.Running |= 0x10;

            return;
        }

        private uint muldiv64round(uint Multiplicand, uint Multiplier, uint Divisor)
        {
            // Yes, I'm correctly rounding the values.
            return (uint)(((ulong)Multiplicand * Multiplier + Multiplier / 2) / Divisor);
        }

        public void update(long Counter, byte ChipID, uint samples)
        {
            dac_control chip = DACData[ChipID];
            uint NewPos;
            int RealDataStp;

            //System.Console.WriteLine("DAC update ChipID{0} samples{1} chip.Running{2} ", ChipID, samples, chip.Running);
            if ((chip.Running & 0x80) != 0)   // disabled
                return;
            if ((chip.Running & 0x01) == 0)    // stopped
                return;

            if (chip.Reverse == 0)
                RealDataStp = chip.DataStep;
            else
                RealDataStp = -chip.DataStep;

            if (samples > 0x20)
            {
                // very effective Speed Hack for fast seeking
                NewPos = chip.Step + (samples - 0x10);
                NewPos = muldiv64round(NewPos * chip.DataStep, chip.Frequency, (UInt32)Common.SampleRate);// DAC_SMPL_RATE);
                while (chip.RemainCmds > 0 && chip.Pos < NewPos)
                {
                    chip.Pos += chip.DataStep;
                    chip.RealPos = (uint)((int)chip.RealPos + RealDataStp);
                    chip.RemainCmds--;
                }
            }

            chip.Step += samples;
            // Formula: Step * Freq / SampleRate
            NewPos = muldiv64round(chip.Step * chip.DataStep, chip.Frequency, (UInt32)Common.SampleRate);// DAC_SMPL_RATE);
            //System.Console.Write("NewPos{0} chip.Step{1} chip.DataStep{2} chip.Frequency{3} DAC_SMPL_RATE{4} \n", NewPos, chip.Step, chip.DataStep, chip.Frequency, (UInt32)common.SampleRate);
            sendCommand(Counter, chip);

            while (chip.RemainCmds > 0 && chip.Pos < NewPos)
            {
                sendCommand(Counter, chip);
                chip.Pos += chip.DataStep;
                //if(model== enmModel.RealModel)                log.Write(string.Format("datastep:{0}",chip.DataStep));
                chip.RealPos = (uint)((int)chip.RealPos + RealDataStp);
                chip.Running &= 0xef;// ~0x10;
                chip.RemainCmds--;
            }

            if (chip.RemainCmds == 0 && ((chip.Running & 0x04) > 0))
            {
                // loop back to start
                chip.RemainCmds = chip.CmdsToSend;
                chip.Step = 0x00;
                chip.Pos = 0x00;
                if (chip.Reverse == 0)
                    chip.RealPos = 0x00;
                else
                    chip.RealPos = (chip.CmdsToSend - 0x01) * chip.DataStep;
            }

            if (chip.RemainCmds == 0)
                chip.Running &= 0xfe;// ~0x01; // stop

            return;
        }

        public byte device_start_daccontrol(byte ChipID)
        {
            dac_control chip;

            if (ChipID >= MAX_CHIPS)
                return 0;

            chip = DACData[ChipID];

            chip.DstChipType = 0xFF;
            chip.DstChipID = 0x00;
            chip.DstCommand = 0x0000;

            chip.Running = 0xFF;   // disable all actions (except setup_chip)

            return 1;
        }

        public void device_reset_daccontrol(byte ChipID)
        {
            dac_control chip = DACData[ChipID];

            chip.DstChipType = 0x00;
            chip.DstChipID = 0x00;
            chip.DstCommand = 0x00;
            chip.CmdSize = 0x00;

            chip.Frequency = 0;
            chip.DataLen = 0x00;
            chip.Data = null;
            chip.DataStart = 0x00;
            chip.StepSize = 0x00;
            chip.StepBase = 0x00;

            chip.Running = 0x00;
            chip.Reverse = 0x00;
            chip.Step = 0x00;
            chip.Pos = 0x00;
            chip.RealPos = 0x00;
            chip.RemainCmds = 0x00;
            chip.DataStep = 0x00;

            return;
        }

        public void setup_chip(byte ChipID, byte ChType, byte ChNum, uint Command)
        {
            dac_control chip = DACData[ChipID];

            chip.DstChipType = ChType; // TypeID (e.g. 0x02 for YM2612)
            chip.DstChipID = ChNum;    // chip number (to send commands to 1st or 2nd chip)
            chip.DstCommand = Command; // Port and Command (would be 0x02A for YM2612)

            switch (chip.DstChipType)
            {
                case 0x00:  // SN76496
                    if ((chip.DstCommand & 0x0010) > 0)
                        chip.CmdSize = 0x01;   // Volume Write
                    else
                        chip.CmdSize = 0x02;   // Frequency Write
                    break;
                case 0x02:  // YM2612
                    chip.CmdSize = 0x01;
                    break;
                case 0x11:  // PWM
                case 0x1F:  // QSound
                    chip.CmdSize = 0x02;
                    break;
                default:
                    chip.CmdSize = 0x01;
                    break;
            }
            chip.DataStep = (byte)(chip.CmdSize * chip.StepSize);

            return;
        }

        public void set_data(byte ChipID, byte[] Data, uint DataLen, byte StepSize, byte StepBase)
        {
            dac_control chip = DACData[ChipID];

            if ((chip.Running & 0x80) > 0)
                return;

            if (DataLen > 0 && Data != null)
            {
                chip.DataLen = DataLen;
                chip.Data = Data;
            }
            else
            {
                chip.DataLen = 0x00;
                chip.Data = null;
            }
            chip.StepSize = (byte)(StepSize > 0 ? StepSize : 1);
            chip.StepBase = StepBase;
            chip.DataStep = (byte)(chip.CmdSize * chip.StepSize);

            return;
        }

        public void refresh_data(byte ChipID, byte[] Data, uint DataLen)
        {
            // Should be called to fix the data pointer. (e.g. after a realloc)
            dac_control chip = DACData[ChipID];

            if ((chip.Running & 0x80) > 0)
                return;

            if (DataLen > 0 && Data != null)
            {
                chip.DataLen = DataLen;
                chip.Data = Data;
            }
            else
            {
                chip.DataLen = 0x00;
                chip.Data = null;
            }

            return;
        }

        public void set_frequency(byte ChipID, uint Frequency)
        {
            //System.Console.WriteLine("ChipID{0} Frequency{1}", ChipID, Frequency);
            dac_control chip = DACData[ChipID];

            if ((chip.Running & 0x80) > 0)
                return;

            chip.Frequency = Frequency;

            return;
        }

        public void start(byte ChipID, uint DataPos, byte LenMode, uint Length,outDatum od)
        {
            dac_control chip = DACData[ChipID];
            chip.od = od;

            uint CmdStepBase;

            if ((chip.Running & 0x80) > 0)
                return;

            CmdStepBase = (uint)(chip.CmdSize * chip.StepBase);
            if (DataPos != 0xFFFFFFFF)  // skip setting DataStart, if Pos == -1
            {
                chip.DataStart = DataPos + CmdStepBase;
                if (chip.DataStart > chip.DataLen)    // catch bad value and force silence
                    chip.DataStart = chip.DataLen;
            }

            switch (LenMode & 0x0F)
            {
                case DCTRL_LMODE_IGNORE:    // Length is already set - ignore
                    break;
                case DCTRL_LMODE_CMDS:      // Length = number of commands
                    chip.CmdsToSend = Length;
                    break;
                case DCTRL_LMODE_MSEC:      // Length = time in msec
                    chip.CmdsToSend = 1000 * Length / chip.Frequency;
                    break;
                case DCTRL_LMODE_TOEND:     // play unti stop-command is received (or data-end is reached)
                    chip.CmdsToSend = (chip.DataLen - (chip.DataStart - CmdStepBase)) / chip.DataStep;
                    break;
                case DCTRL_LMODE_BYTES:     // raw byte count
                    chip.CmdsToSend = Length / chip.DataStep;
                    break;
                default:
                    chip.CmdsToSend = 0x00;
                    break;
            }
            chip.Reverse = (byte)((LenMode & 0x10) >> 4);

            chip.RemainCmds = chip.CmdsToSend;
            chip.Step = 0x00;
            chip.Pos = 0x00;
            if (chip.Reverse == 0)
                chip.RealPos = 0x00;
            else
                chip.RealPos = (chip.CmdsToSend - 0x01) * chip.DataStep;

            chip.Running &= 0xfb;// ~0x04;
            chip.Running |= (byte)((LenMode & 0x80) > 0 ? 0x04 : 0x00);    // set loop mode

            chip.Running |= 0x01;  // start
            chip.Running &= 0xef;// ~0x10; // command isn't yet sent

            return;
        }

        public void stop(byte ChipID)
        {
            dac_control chip = DACData[ChipID];

            if ((chip.Running & 0x80) > 0)
                return;

            chip.Running &= 0xfe;// ~0x01; // stop

            return;
        }

        private void chip_reg_write(long Counter, byte ChipType, byte ChipID, byte Port, byte Offset, byte Data,outDatum od)
        {
            switch (ChipType)
            {
                case 0x02:  // YM2612
                    chipRegister.YM2612SetRegister(od, Counter, ChipID, Port, Offset, Data);
                    //log.Write(string.Format("Cnt{0} Data{1}",Counter,Data));
                    break;
                case 0x10:
                    chipRegister.RF5C164SetRegister(od, Counter, ChipID, Offset, Data);
                    break;
                case 0x11:  // PWM
                    chipRegister.writePWM(ChipID, Port, (uint)((Offset << 8) | (Data << 0)));
                    break;
                case 0x17:  // OKIM6258
                    if (model == EnmModel.VirtualModel) //System.Console.Write("[DAC]");
                        chipRegister.writeOKIM6258(ChipID, Offset, Data);
                    break;
                case 0x1b:  // HuC6280
                    chipRegister.HuC6280SetRegister(od, Counter, ChipID, Offset, Data);
                    break;
            }
        }

        public void refresh()
        {
            for (int i = 0; i < MAX_CHIPS; i++) DACData[i] = new dac_control();
        }

        public class dac_control
        {
            // Commands sent to dest-chip
            public byte DstChipType;
            public byte DstChipID;
            public uint DstCommand;
            public byte CmdSize;

            public uint Frequency;   // Frequency (Hz) at which the commands are sent
            public uint DataLen;     // to protect from reading beyond End Of Data
            public byte[] Data;
            public uint DataStart;   // Position where to start
            public byte StepSize;     // usually 1, set to 2 for L/R interleaved data
            public byte StepBase;     // usually 0, set to 0/1 for L/R interleaved data
            public uint CmdsToSend;

            // Running Bits:	0 (01) - is playing
            //					2 (04) - loop sample (simple loop from start to end)
            //					4 (10) - already sent this command
            //					7 (80) - disabled
            public byte Running;
            public byte Reverse;
            public uint Step;        // Position in Player SampleRate
            public uint Pos;         // Position in Data SampleRate
            public uint RemainCmds;
            public uint RealPos;     // true Position in Data (== Pos, if Reverse is off)
            public byte DataStep;     // always StepSize * CmdSize
            public outDatum od;
        }

    }
}
