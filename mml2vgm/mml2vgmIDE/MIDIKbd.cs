﻿using Core;
using musicDriverInterface;
using NAudio.Midi;
using SoundManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mml2vgmIDE
{
    public class MIDIKbd
    {
        private MDChipParams.MIDIKbd newParam = null;
        private MDChipParams.MIDIKbd oldParam = new MDChipParams.MIDIKbd();
        private Mml2vgm mv = null;
        private partWork pw;
        private ClsChip cChip = null;
        private Chip eChip = null;
        private SoundManager.SoundManager SoundManager = null;
        private Setting setting = null;
        private MidiIn midiin = null;
        private byte[] noteFlg = new byte[164];
        private int latestNoteNumberMONO = -1;
        private int[] shiftTbl = new int[] { 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0 };
        private Queue<MML> qMML = new Queue<MML>();

        //キーボード入力向け
        //private Keys[] kbdTbl = new Keys[] {
        //    Keys.Q,Keys.W //  Q (Octave down) W (Octave up)
        //    ,Keys.Attn,Keys.Oem4 //  @ (Octave down) [ (Octave up)
        //    ,Keys.A,Keys.Z,Keys.S,Keys.X //  A Z S X
        //    ,Keys.C,Keys.F,Keys.V,Keys.G,Keys.B //  C F V G B
        //    ,Keys.N,Keys.J,Keys.M,Keys.K,Keys.Oemcomma,Keys.L,Keys.OemPeriod //  N J M K , L .
        //    ,Keys.Oem2,Keys.Oem1 //  / :
        //};
        //private bool[] keyPress = null;


        public MIDIKbd(Setting setting, MDChipParams.MIDIKbd newParam)
        {
            try
            {
                this.setting = setting;
                //keyPress = new bool[kbdTbl.Length];
                if (setting.midiKbd.Octave == 0) setting.midiKbd.Octave = 4;
                this.newParam = newParam;
                Init();
            }
            catch
            {

            }
        }

        public void StartMIDIInMonitoring()
        {

            if (setting.midiKbd.MidiInDeviceName == "")
            {
                return;
            }

            if (midiin != null)
            {
                try
                {
                    midiin.Stop();
                    midiin.Dispose();
                    midiin.MessageReceived -= midiIn_MessageReceived;
                    midiin.ErrorReceived -= midiIn_ErrorReceived;
                    midiin = null;
                }
                catch
                {
                    midiin = null;
                }
            }

            if (midiin == null)
            {
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    if (setting.midiKbd.MidiInDeviceName == MidiIn.DeviceInfo(i).ProductName)
                    {
                        try
                        {
                            midiin = new MidiIn(i);
                            midiin.MessageReceived += midiIn_MessageReceived;
                            midiin.ErrorReceived += midiIn_ErrorReceived;
                            midiin.Start();
                        }
                        catch
                        {
                            midiin = null;
                        }
                    }
                }
            }

        }

        public void StopMIDIInMonitoring()
        {
            if (midiin != null)
            {
                try
                {
                    midiin.Stop();
                    midiin.Dispose();
                    midiin.MessageReceived -= midiIn_MessageReceived;
                    midiin.ErrorReceived -= midiIn_ErrorReceived;
                    midiin = null;
                }
                catch
                {
                    midiin = null;
                }
            }
        }




        private void Init()
        {
            string txt = Properties.Resources.tmpMIDIKbd;
            txt = string.Format(
                txt
                , newParam.cClockCnt < 1 ? 192 : newParam.cClockCnt
                , newParam.cTempo < 1 ? 177 : newParam.cTempo
                );
            string[] text = txt.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            string stPath = System.Windows.Forms.Application.StartupPath;
            mv = new Mml2vgm(null, text, "", "", stPath, dmyDisp, "", false);
            mv.isIDE = true;
            mv.Start();
            mv.desVGM.isRealTimeMode = true;

            if (mv.desVGM.ym2608 == null) return;
            if (mv.desVGM.ym2608[0] == null) return;
            if (mv.desVGM.ym2608[0].lstPartWork[0] == null) return;

            cChip = mv.desVGM.ym2608[0];
            cChip.use = true;
            pw = cChip.lstPartWork[0];

            SoundManager = Audio.sm;
            SoundManager.AddDataSeqFrqEvent(OnDataSeqFrq);
            SoundManager.CurrentChip = "YM2608";
            SoundManager.CurrentCh = 1;

        }

        private void dmyDisp(string dmy)
        {
            log.Write(dmy);
        }

        private void OnDataSeqFrq(long SeqCounter)
        {
            if (mv == null) return;
            if (mv.desVGM == null) return;

            //if ((Audio.sm.Mode & SendMode.MML) != SendMode.MML)
            //{
            //    if (rtMML != null)
            //        rtMML.OneFrameSeq();
            //}

            if (pw.apg == null) return;
            eChip = Audio.GetChip(EnmChip.YM2608);
            if (eChip == null) return;

            //入力時に生じた、各チップ向けの送信データをコンパイラを使用して生成する。
            if (qMML.Count < 1) return;
            while (qMML.Count > 0)
            {
                MML mml = qMML.Dequeue();
                switch (mml.type)
                {
                    case enmMMLType.Octave:
                        cChip.CmdOctave(pw.apg, mml);
                        break;
                    case enmMMLType.Note:
                        if ((int)mml.args[1] >= 0)
                        {
                            cChip.CmdNote(pw, pw.apg, mml);//TODO:page制御やってない
                            cChip.MultiChannelCommand(mml);
                        }
                        else
                        {
                            cChip.SetKeyOff(pw.apg, mml);
                        }
                        break;
                }

            }

            //生成したデータを取得
            mv.desVGM.dat.Clear();
            List<outDatum> dat = pw.apg.sendData;

            //音源への送信データキューへ追加
            Enq enq = SoundManager.GetDriverDataEnqueue();
            while (0 < dat.Count)
            {
                outDatum od = dat[0];
                if (od == null)
                {
                    dat.RemoveAt(0);
                    continue;
                }

                byte val = od.val;
                byte adr;
                byte prm;
                switch (val)
                {
                    case 0x52://OPN2
                        adr = dat[1].val;
                        prm = dat[2].val;
                        enq(dat[0], SeqCounter, eChip, EnmDataType.Force, adr, prm, null);
                        dat.RemoveAt(0);
                        dat.RemoveAt(0);
                        dat.RemoveAt(0);
                        break;
                    case 0x56://OPNA
                        adr = dat[1].val;
                        prm = dat[2].val;
                        enq(dat[0], 0, eChip, EnmDataType.Force, adr, prm, null);
                        dat.RemoveAt(0);
                        dat.RemoveAt(0);
                        dat.RemoveAt(0);
                        break;
                    default:
                        dat.RemoveAt(0);
                        break;
                }
            }
        }

        private void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            log.ForcedWrite(String.Format("Error Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (!setting.midiKbd.UseMIDIKeyboard) return;

            NoteEvent ne;
            switch (e.MidiEvent.CommandCode)
            {
                case MidiCommandCode.NoteOn:
                    if (e.MidiEvent is NoteOnEvent)
                    {
                        NoteOnEvent noe = (NoteOnEvent)e.MidiEvent;
                        NoteOn(noe.NoteNumber, 127);// noe.Velocity);
                    }
                    else if (e.MidiEvent is NoteEvent)
                    {
                        ne = (NoteEvent)e.MidiEvent;
                        if (ne.Velocity == 0)
                        {
                            NoteOff(ne.NoteNumber);
                        }
                    }
                    break;
                case MidiCommandCode.NoteOff:
                    ne = (NoteEvent)e.MidiEvent;
                    NoteOff(ne.NoteNumber);
                    break;
                case MidiCommandCode.ControlChange:
                    break;
            }
        }

        private void NoteOn(int n, int velocity)
        {
            noteFlg[n & 0x7f] = (byte)(velocity & 0x7f);
            log.Write(string.Format("MIDIKbd:Note On{0}", n));

            if (setting.midiKbd.IsMONO) NoteOnMONO(n, velocity);
            else NoteOnPOLY(n, velocity);
        }

        private void NoteOnPOLY(int n, int velocity)
        {
            throw new NotImplementedException();
        }

        private void NoteOnMONO(int n, int velocity)
        {
            if (n < 0 || n > 127) return;

            NoteOffMONO(latestNoteNumberMONO);
            MML mml = MakeMML_Octave(n);
            qMML.Enqueue(mml);
            //cChip.CmdOctave(pw.apg, mml);
            mml = MakeMML_NoteOn(n);
            qMML.Enqueue(mml);
            //lock (lockObject)
            //{
            //    cChip.CmdNote(pw, pw.apg, mml);//TODO:page制御やってない
            //    cChip.MultiChannelCommand(mml);
            //}

            latestNoteNumberMONO = n;
        }

        private void NoteOff(int n)
        {
            noteFlg[n & 0x7f] = 0;
            log.Write(string.Format("MIDIKbd:Note Off{0}", n));

            if (setting.midiKbd.IsMONO) NoteOffMONO(n);
            else NoteOffPOLY(n);
        }

        private void NoteOffPOLY(int n)
        {
            throw new NotImplementedException();
        }

        private void NoteOffMONO(int n)
        {
            if (n < 0 || n > 127) return;
            if (latestNoteNumberMONO != n) return;
            MML mml = MakeMML_NoteOff(n);
            qMML.Enqueue(mml);
        }

        private MML MakeMML_Octave(int n)
        {
            MML mml = new MML();
            mml.type = enmMMLType.Octave;
            mml.line = null;
            mml.column = -1;
            mml.args = new List<object>();
            mml.args.Add(n / 12);

            return mml;
        }

        private MML MakeMML_NoteOn(int n)
        {
            MML mml = new MML();
            mml.type = enmMMLType.Note;
            mml.line = null;
            mml.column = -1;
            mml.args = new List<object>();
            Note note = new Note();
            mml.args.Add(note);
            note.cmd = "ccddeffggaab"[n % 12];
            note.shift = shiftTbl[n % 12];
            note.length = 1;
            mml.args.Add(n);

            return mml;
        }

        private MML MakeMML_NoteOff(int n)
        {
            MML mml = new MML();
            mml.type = enmMMLType.Note;
            mml.line = null;
            mml.column = -1;
            mml.args = new List<object>();
            Note note = new Note();
            mml.args.Add(note);
            note.cmd = "ccddeffggaab"[n % 12];
            note.shift = shiftTbl[n % 12];
            note.length = 1;
            mml.args.Add(-n);

            return mml;
        }

    }
}
