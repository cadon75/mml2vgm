﻿using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mml2vgmIDE.Driver.ZGM.ZgmChip
{
    public class YM2609 : ZgmChip
    {

        public YM2609(ChipRegister chipRegister, Setting setting, outDatum[] vgmBuf)
        {
            this.chipRegister = chipRegister;
            this.setting = setting;
            this.vgmBuf = vgmBuf;

            Use = true;
            Device = EnmZGMDevice.YM2609;
            name = "YM2609";
            Model = EnmVRModel.None;
            Number = 0;
            Hosei = 0;
        }

        public override void Setup(int chipIndex, ref uint dataPos, ref Dictionary<int, Driver.ZGM.zgm.RefAction<outDatum, uint>> cmdTable)
        {
            base.Setup(chipIndex, ref dataPos, ref cmdTable);

            if (cmdTable.ContainsKey(defineInfo.commandNo)) cmdTable.Remove(defineInfo.commandNo);
            cmdTable.Add(defineInfo.commandNo, SendPort0);

            if (cmdTable.ContainsKey(defineInfo.commandNo + 1)) cmdTable.Remove(defineInfo.commandNo + 1);
            cmdTable.Add(defineInfo.commandNo + 1, SendPort1);

            if (cmdTable.ContainsKey(defineInfo.commandNo + 2)) cmdTable.Remove(defineInfo.commandNo + 2);
            cmdTable.Add(defineInfo.commandNo + 2, SendPort2);

            if (cmdTable.ContainsKey(defineInfo.commandNo + 3)) cmdTable.Remove(defineInfo.commandNo + 3);
            cmdTable.Add(defineInfo.commandNo + 3, SendPort3);
        }

        private void SendPort0(outDatum od, ref uint vgmAdr)
        {
            chipRegister.YM2609SetRegister(od, Audio.DriverSeqCounter, Index, 0, vgmBuf[vgmAdr + 1].val, vgmBuf[vgmAdr + 2].val);
            vgmAdr += 3;
        }

        private void SendPort1(outDatum od, ref uint vgmAdr)
        {
            chipRegister.YM2609SetRegister(od, Audio.DriverSeqCounter, Index, 1, vgmBuf[vgmAdr + 1].val, vgmBuf[vgmAdr + 2].val);
            vgmAdr += 3;
        }

        private void SendPort2(outDatum od, ref uint vgmAdr)
        {
            chipRegister.YM2609SetRegister(od, Audio.DriverSeqCounter, Index, 2, vgmBuf[vgmAdr + 1].val, vgmBuf[vgmAdr + 2].val);
            vgmAdr += 3;
        }

        private void SendPort3(outDatum od, ref uint vgmAdr)
        {
            chipRegister.YM2609SetRegister(od, Audio.DriverSeqCounter, Index, 3, vgmBuf[vgmAdr + 1].val, vgmBuf[vgmAdr + 2].val);
            vgmAdr += 3;
        }

    }
}