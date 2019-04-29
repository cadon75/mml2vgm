﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Core;
//using Jacobi.Vst.Interop.Host;
//using Jacobi.Vst.Core;
using System.IO;
using System.IO.Compression;
//using mml2vgmIDE.form;
using SoundManager;
using MDSound;

namespace mml2vgmIDE
{
    public class Audio
    {

        public static int clockAY8910 = 1789750;
        public static int clockK051649 = 1500000;
        public static int clockC140 = 21390;
        public static int clockC352 = 24192000;
        public static int clockFDS = 0;
        public static int clockHuC6280 = 0;
        public static int clockRF5C164 = 0;
        public static int clockMMC5 = 0;
        public static int clockNESDMC = 0;
        public static int clockOKIM6258 = 0;
        public static int clockOKIM6295 = 0;
        public static int clockSegaPCM = 0;
        public static int clockSN76489 = 0;
        public static int clockYM2151 = 0;
        public static int clockYM2203 = 0;
        public static int clockYM2413 = 0;
        public static int clockYM2608 = 0;
        public static int clockYM2610 = 0;
        public static int clockYM2612 = 0;
        public static int clockYMF278B = 0;

        private static object lockObj = new object();
        private static bool _fatalError = false;
        public static bool fatalError
        {
            get
            {
                lock (lockObj)
                {
                    return _fatalError;
                }
            }

            set
            {
                lock (lockObj)
                {
                    _fatalError = value;
                }
            }
        }

        private static uint samplingBuffer = 1024;
        private static MDSound.MDSound mds = null;
        public static MDSound.MDSound mdsMIDI = null;
        private static NAudioWrap naudioWrap;
        //private static WaveWriter waveWriter = null;


        //private static RSoundChip[] scAY8910 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2612 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scSN76489 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2151 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2608 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2203 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2413 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2610 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2610EA = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scYM2610EB = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scC140 = new RSoundChip[2] { null, null };
        //private static RSoundChip[] scSEGAPCM = new RSoundChip[2] { null, null };
        public static RealChip realChip;
        private static ChipRegister chipRegister = null;
        public static HashSet<EnmChip> useChip = new HashSet<EnmChip>();


        public static bool trdClosed = false;
        private static bool _trdStopped = true;
        public static bool trdStopped
        {
            get
            {
                lock (lockObj)
                {
                    return _trdStopped;
                }
            }
            set
            {
                lock (lockObj)
                {
                    _trdStopped = value;
                }
            }
        }
        private static Stopwatch sw = Stopwatch.StartNew();
        private static double swFreq = Stopwatch.Frequency;

        private static outDatum[] vgmBuf = null;
        private static double vgmSpeed;

        public static double fadeoutCounter;
        public static double fadeoutCounterEmu;
        private static double fadeoutCounterDelta;

        private static bool Paused = false;
        public static bool Stopped = false;
        private static int StepCounter = 0;

        private static Setting setting = null;

        public static baseDriver driver = null;

        private static bool hiyorimiNecessary = false;

        public static ChipLEDs chipLED = new ChipLEDs();
        public static VisVolume visVolume = new VisVolume();

        private static int MasterVolume = 0;
        private static byte[] chips = new byte[256];
        private static string PlayingFileName;
        private static string PlayingArcFileName;
        private static int MidiMode = 0;
        private static int SongNo = 0;
        private static List<Tuple<string, byte[]>> ExtendFile = null;
        private static EnmFileFormat PlayingFileFormat;

        private static System.Diagnostics.Stopwatch stwh = System.Diagnostics.Stopwatch.StartNew();
        public static double ProcTimePer1Frame = 0;

        //private static List<vstInfo2> vstPlugins = new List<vstInfo2>();
        //private static List<vstInfo2> vstPluginsInst = new List<vstInfo2>();

        //private static List<NAudio.Midi.MidiOut> midiOuts = new List<NAudio.Midi.MidiOut>();
        //private static List<int> midiOutsType = new List<int>();
        //private static List<vstInfo2> vstMidiOuts = new List<vstInfo2>();
        //private static List<int> vstMidiOutsType = new List<int>();
        public static string errMsg = "";
        public static bool flgReinit = false;
        private static short[] bufVirtualFunction_MIDIKeyboard = null;
        private static byte[] mmc5regs = new byte[10];

        public static SoundManager.SoundManager sm = null;
        private static Enq enq;
        private static RingBuffer emuRecvBuffer = null;
        public static long DriverSeqCounter=0;
        public static long EmuSeqCounter=0;




        public static void Init(Setting setting)
        {
            log.ForcedWrite("Audio:Init:Begin");

            log.ForcedWrite("Audio:Init:STEP 01");
            naudioWrap = new NAudioWrap((int)Common.SampleRate, trdVgmVirtualFunction);
            naudioWrap.PlaybackStopped += NaudioWrap_PlaybackStopped;



            log.ForcedWrite("Audio:Init:STEP 02");
            Audio.setting = setting;
          //  waveWriter = new WaveWriter(setting);



            log.ForcedWrite("Audio:Init:STEP 03");
            {
                log.ForcedWrite("Audio:Init:STEP 03:Init MDSound");
                if (mds == null)
                    mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, null);
                else
                    mds.Init((UInt32)Common.SampleRate, samplingBuffer, null);

                log.ForcedWrite("Audio:Init:STEP 03:Init MDSound(OPN2 midi)");
                List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();
                MDSound.MDSound.Chip chip;
                ym2612 ym2612 = new ym2612();
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
                chip.ID = (byte)0;
                chip.Instrument = ym2612;
                chip.Update = ym2612.Update;
                chip.Start = ym2612.Start;
                chip.Stop = ym2612.Stop;
                chip.Reset = ym2612.Reset;
                chip.SamplingRate = (UInt32)Common.SampleRate;
                chip.Volume = setting.balance.YM2612Volume;
                chip.Clock = 7670454;
                chip.Option = null;
                chipLED.PriOPN2 = 1;
                lstChips.Add(chip);
                if (mdsMIDI == null) mdsMIDI = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
                else mdsMIDI.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
            }



            log.ForcedWrite("Audio:Init:STEP 04");
            {
                if (realChip == null) realChip = new RealChip();

                chipRegister = new ChipRegister(setting, mds, realChip);

                //RealChipManualDetect(setting);

                chipRegister.initChipRegister(null);
            }



            log.ForcedWrite("Audio:Init:STEP 05");
            Paused = false;
            Stopped = true;
            fatalError = false;
            //oneTimeReset = false;



            //log.ForcedWrite("Audio:Init:STEP 06");
            //{
            //    log.ForcedWrite("Audio:Init:VST:STEP 01");
            //    vstparse();
            //    log.ForcedWrite("Audio:Init:VST:STEP 02"); //Load VST instrument
            //                                               //複数のmidioutの設定から必要なVSTを絞り込む
            //    Dictionary<string, int> dicVst = new Dictionary<string, int>();
            //    if (setting.midiOut.lstMidiOutInfo != null)
            //    {
            //        foreach (midiOutInfo[] aryMoi in setting.midiOut.lstMidiOutInfo)
            //        {
            //            if (aryMoi == null) continue;
            //            Dictionary<string, int> dicVst2 = new Dictionary<string, int>();
            //            foreach (midiOutInfo moi in aryMoi)
            //            {
            //                if (!moi.isVST) continue;
            //                if (dicVst2.ContainsKey(moi.fileName))
            //                {
            //                    dicVst2[moi.fileName]++;
            //                    continue;
            //                }
            //                dicVst2.Add(moi.fileName, 1);
            //            }

            //            foreach (var kv in dicVst2)
            //            {
            //                if (dicVst.ContainsKey(kv.Key))
            //                {
            //                    if (dicVst[kv.Key] < kv.Value)
            //                    {
            //                        dicVst[kv.Key] = kv.Value;
            //                    }
            //                    continue;
            //                }
            //                dicVst.Add(kv.Key, kv.Value);
            //            }
            //        }
            //    }
            //    foreach (var kv in dicVst)
            //    {
            //        for (int i = 0; i < kv.Value; i++)
            //        {
            //            VstPluginContext ctx = OpenPlugin(kv.Key);
            //            if (ctx == null) continue;

            //            vstInfo2 vi = new vstInfo2();
            //            vi.key = DateTime.Now.Ticks.ToString();
            //            Thread.Sleep(1);
            //            vi.vstPlugins = ctx;
            //            vi.fileName = kv.Key;
            //            vi.isInstrument = true;

            //            ctx.PluginCommandStub.SetBlockSize(512);
            //            ctx.PluginCommandStub.SetSampleRate(Common.SampleRate);
            //            ctx.PluginCommandStub.MainsChanged(true);
            //            ctx.PluginCommandStub.StartProcess();
            //            vi.effectName = ctx.PluginCommandStub.GetEffectName();
            //            vi.editor = true;

            //            if (vi.editor)
            //            {
            //                frmVST dlg = new frmVST();
            //                dlg.PluginCommandStub = ctx.PluginCommandStub;
            //                dlg.Show(vi);
            //                vi.vstPluginsForm = dlg;
            //            }

            //            vstPluginsInst.Add(vi);
            //        }
            //    }
            //    if (setting.vst != null && setting.vst.VSTInfo != null)
            //    {

            //        log.ForcedWrite("Audio:Init:VST:STEP 03"); //Load VST Effect

            //        for (int i = 0; i < setting.vst.VSTInfo.Length; i++)
            //        {
            //            if (setting.vst.VSTInfo[i] == null) continue;
            //            VstPluginContext ctx = OpenPlugin(setting.vst.VSTInfo[i].fileName);
            //            if (ctx == null) continue;

            //            vstInfo2 vi = new vstInfo2();
            //            vi.vstPlugins = ctx;
            //            vi.fileName = setting.vst.VSTInfo[i].fileName;
            //            vi.key = setting.vst.VSTInfo[i].key;

            //            ctx.PluginCommandStub.SetBlockSize(512);
            //            ctx.PluginCommandStub.SetSampleRate(Common.SampleRate / 1000.0f);
            //            ctx.PluginCommandStub.MainsChanged(true);
            //            ctx.PluginCommandStub.StartProcess();
            //            vi.effectName = ctx.PluginCommandStub.GetEffectName();
            //            vi.power = setting.vst.VSTInfo[i].power;
            //            vi.editor = setting.vst.VSTInfo[i].editor;
            //            vi.location = setting.vst.VSTInfo[i].location;
            //            vi.param = setting.vst.VSTInfo[i].param;

            //            if (vi.editor)
            //            {
            //                frmVST dlg = new frmVST();
            //                dlg.PluginCommandStub = ctx.PluginCommandStub;
            //                dlg.Show(vi);
            //                vi.vstPluginsForm = dlg;
            //            }

            //            if (vi.param != null)
            //            {
            //                for (int p = 0; p < vi.param.Length; p++)
            //                {
            //                    ctx.PluginCommandStub.SetParameter(p, vi.param[p]);
            //                }
            //            }

            //            vstPlugins.Add(vi);
            //        }


            //    }
            //}



            //log.ForcedWrite("Audio:Init:STEP 07");
            //midi outをリリース
            //ReleaseAllMIDIout();



            //log.ForcedWrite("Audio:Init:STEP 08");
            //midi out のインスタンスを作成
            //MakeMIDIout(setting, 1);
            //chipRegister.resetAllMIDIout();



            log.ForcedWrite("Audio:Init:STEP 09");
            naudioWrap.Start(Audio.setting);



            log.ForcedWrite("Audio:Init:STEP 10");
            SoundManagerMount();



            log.ForcedWrite("Audio:Init:Complete");



        }

        public static void RealChipManualDetect(Setting setting)
        {
            chipRegister.SetRealChipInfo(EnmDevice.AY8910, setting.AY8910Type, setting.AY8910SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.C140, setting.C140Type, setting.C140SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.SegaPCM, setting.SEGAPCMType, setting.SEGAPCMSType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.SN76489, setting.SN76489Type, setting.SN76489SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2151, setting.YM2151Type, setting.YM2151SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2203, setting.YM2203Type, setting.YM2203SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2413, setting.YM2413Type, setting.YM2413SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2608, setting.YM2608Type, setting.YM2608SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2610, setting.YM2610Type, setting.YM2610SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM2612, setting.YM2612Type, setting.YM2612SType, setting.LatencyEmulation, setting.LatencySCCI);

            chipRegister.SetRealChipInfo(EnmDevice.HuC6280, setting.HuC6280Type, setting.HuC6280SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.Y8950, setting.Y8950Type, setting.Y8950SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YM3526, setting.YM3526Type, setting.YM3526SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YMF262, setting.YMF262Type, setting.YMF262SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YMF271, setting.YMF271Type, setting.YMF271SType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YMF278B, setting.YMF278BType, setting.YMF278BSType, setting.LatencyEmulation, setting.LatencySCCI);
            chipRegister.SetRealChipInfo(EnmDevice.YMZ280B, setting.YMZ280BType, setting.YMZ280BSType, setting.LatencyEmulation, setting.LatencySCCI);
        }

        public static void RealChipAutoDetect(Setting setting)
        {
            Setting.ChipType[] chipType = new Setting.ChipType[2];
            List<Setting.ChipType> ret = realChip.GetRealChipList();

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.AY8910[i].Use) continue;
                chipRegister.AY8910[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.AY8910, chipRegister.AY8910[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.AY8910, chipRegister.AY8910[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.AY8910, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.C140[i].Use) continue;
                chipRegister.C140[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.C140, chipRegister.C140[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.C140, chipRegister.C140[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.C140, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.SEGAPCM[i].Use) continue;
                chipRegister.SEGAPCM[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.SegaPCM, chipRegister.SEGAPCM[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.SegaPCM, chipRegister.SEGAPCM[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.SegaPCM, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.SN76489[i].Use) continue;
                chipRegister.SN76489[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.SN76489, chipRegister.SN76489[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.SN76489, chipRegister.SN76489[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.SN76489, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2151[i].Use) continue;
                chipRegister.YM2151[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseEmu2 = true;
                chipType[i].UseEmu3 = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2151, chipRegister.YM2151[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2151, chipRegister.YM2151[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2151, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2203[i].Use) continue;
                chipRegister.YM2203[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2203, chipRegister.YM2203[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2203, chipRegister.YM2203[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2203, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2413[i].Use) continue;
                chipRegister.YM2413[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2413, chipRegister.YM2413[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2413, chipRegister.YM2413[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2413, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2608[i].Use) continue;
                chipRegister.YM2608[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2608, chipRegister.YM2608[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2608, chipRegister.YM2608[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2608, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2610[i].Use) continue;
                chipRegister.YM2610[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2610, chipRegister.YM2610[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2610, chipRegister.YM2610[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2610, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

            for (int i = 0; i < 2; i++)
            {
                chipType[i] = new Setting.ChipType();
                if (!chipRegister.YM2612[i].Use) continue;
                chipRegister.YM2612[i].Model = EnmModel.VirtualModel;
                chipType[i].UseEmu = true;
                chipType[i].UseEmu2 = true;
                chipType[i].UseEmu3 = false;
                chipType[i].UseScci = false;
                if (ret.Count == 0) continue;
                SearchRealChip(chipType, ret, i, EnmDevice.YM2612, chipRegister.YM2612[i], setting.AutoDetectModuleType == 0 ? 0 : 1);
                if (chipType[i].UseEmu) SearchRealChip(chipType, ret, i, EnmDevice.YM2612, chipRegister.YM2612[i], setting.AutoDetectModuleType == 0 ? 1 : 0);
            }
            chipRegister.SetRealChipInfo(EnmDevice.YM2612, chipType[0], chipType[1], setting.LatencyEmulation, setting.LatencySCCI);

        }

        private static void SearchRealChip(Setting.ChipType[] chipType, List<Setting.ChipType> ret, int i, EnmDevice dev, Chip chip,int ModuleType)
        {
            for (int j = 0; j < ret.Count; j++)
            {
                if (ModuleType == 0)//scci
                {
                    if (ret[j].SoundLocation == -1) continue;
                }
                else
                {
                    if (ret[j].SoundLocation != -1) continue;
                }

                EnmRealModel mdl = CheckRealChip(dev, ret[j]);
                if (mdl != EnmRealModel.unknown)
                {
                    chipType[i] = ret[j];
                    chip.Model = EnmModel.RealModel;
                    chipType[i].UseEmu = false;
                    chipType[i].UseEmu2 = false;
                    chipType[i].UseEmu3 = false;
                    chipType[i].UseScci = true;

                    ret.RemoveAt(j);

                    break;
                }
            }
        }

        private static EnmRealModel CheckRealChip(EnmDevice dev, Setting.ChipType chipType)
        {
            switch (dev)
            {
                case EnmDevice.AY8910:
                    if (chipType.SoundLocation == -1) //GIMIC ?
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2608
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YMF288
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2203)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.AY8910
                            || chipType.Type == (int)EnmRealChipType.YM2203
                            || chipType.Type == (int)EnmRealChipType.YM2608
                            || chipType.Type == (int)EnmRealChipType.YM2610)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.C140:
                    if (chipType.SoundLocation == -1) 
                    {
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.C140)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.SegaPCM:
                    if (chipType.SoundLocation == -1) 
                    {
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.SEGAPCM)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2151:
                    if (chipType.SoundLocation == -1) 
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2151)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2151)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2203:
                    if (chipType.SoundLocation == -1)
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2203
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2608
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2610B
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YMF288)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2203
                            || chipType.Type == (int)EnmRealChipType.YM2608
                            || chipType.Type == (int)EnmRealChipType.YM2610)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2413:
                    if (chipType.SoundLocation == -1)
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2413)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2413)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2608:
                    if (chipType.SoundLocation == -1)
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2608
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YMF288)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2608)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2610:
                    if (chipType.SoundLocation == -1)
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2608
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2610B
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YMF288)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2608
                            || chipType.Type == (int)EnmRealChipType.YM2610)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
                case EnmDevice.YM2612:
                    if (chipType.SoundLocation == -1)
                    {
                        if (chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM2612
                            || chipType.Type == (int)Nc86ctl.ChipType.CHIP_YM3438)
                        {
                            return EnmRealModel.GIMIC;
                        }
                    }
                    else
                    {
                        if (chipType.Type == (int)EnmRealChipType.YM2612)
                        {
                            return EnmRealModel.SCCI;
                        }
                    }
                    break;
            }
            return EnmRealModel.unknown;
        }

        public static void SetVGMBuffer(EnmFileFormat format, outDatum[] srcBuf)
        {
            PlayingFileFormat = format;
            vgmBuf = srcBuf;
        }

        private static void SoundManagerMount()
        {
            sm = new SoundManager.SoundManager();
            DriverAction DriverAction = new DriverAction();
            DriverAction.Init = DriverActionInit;
            DriverAction.Main = DriverActionMain;
            DriverAction.Final = DriverActionFinal;
            sm.Setup(
                DriverAction, RealChipAction
                , chipRegister.ProcessingData
                , DataSeqFrqCallBack, WaitSync
                , null
                , SoundManager.SoundManager.DATA_SEQUENCE_FREQUENCE * setting.LatencyEmulation / 1000
                , SoundManager.SoundManager.DATA_SEQUENCE_FREQUENCE * setting.LatencySCCI / 1000);
            enq = sm.GetDriverDataEnqueue();
            chipRegister.enq = enq;
            emuRecvBuffer = sm.GetEmuRecvBuffer();
        }

        private static void WaitSync()
        {
            log.Write("Reset Audio Device Sync.");
            Thread.Sleep(50);
            //ResetAudioDeviceSync();
            //while (!GetAudioDeviceSync()) { Thread.Sleep(1); }
            ResetAudioDeviceSync();
            while (!GetAudioDeviceSync()) { Thread.Sleep(0); }
            if (sm.GetSeqCounter() > 0)
            {
                log.Write(string.Format("Warn:{0}",sm.GetSeqCounter()));
            }
        }

        private static void DataSeqFrqCallBack(long Counter)
        {

            if (!sm.GetFadeOut()) return;

            //
            // Fadeout 処理
            //

            fadeoutCounter -= fadeoutCounterDelta;
            if (fadeoutCounter < 0.7)
            {
                fadeoutCounter -= fadeoutCounterDelta * 2.0;
            }

            // fadeout完了したら演奏停止
            if (fadeoutCounter <= 0.0)
            {
                fadeoutCounter = 0.0;
                sm.RequestStopAsync();
            }

            chipRegister.SetFadeoutVolume(Counter, fadeoutCounter);
        }

        private static void DriverActionInit()
        {
            ;
        }

        private static void DriverActionMain()
        {
            driver.oneFrameProc();
            if (driver.Stopped)
            {
                sm.RequestStopAtDataMaker();
            }
        }

        private static void DriverActionFinal()
        {
            softReset(DriverSeqCounter);
        }

        private static void RealChipAction(long Counter,Chip Chip, EnmDataType Type, int Address, int Data, object ExData)
        {
            chipRegister.SendChipData(Counter, Chip, Type, Address, Data, ExData);
        }



        //public static List<PlayList.music> getMusic(string file, byte[] buf, string zipFile = null, object entry = null)
        //{
        //    List<PlayList.music> musics = new List<PlayList.music>();
        //    PlayList.music music = new PlayList.music();

        //    music.format = EnmFileFormat.unknown;
        //    music.fileName = file;
        //    music.arcFileName = zipFile;
        //    music.arcType = EnmArcType.unknown;
        //    if (!string.IsNullOrEmpty(zipFile)) music.arcType = zipFile.ToLower().LastIndexOf(".zip") != -1 ? EnmArcType.ZIP : EnmArcType.LZH;
        //    music.title = "unknown";
        //    music.game = "unknown";
        //    music.type = "-";

        //    if (file.ToLower().LastIndexOf(".nrd") != -1)
        //    {

        //        music.format = EnmFileFormat.NRT;
        //        uint index = 42;
        //        GD3 gd3 = (new NRTDRV()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".mdr") != -1)
        //    {

        //        music.format = EnmFileFormat.MDR;
        //        uint index = 0;
        //        GD3 gd3 = (new Driver.MoonDriver.MoonDriver()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackName;
        //        music.titleJ = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".mdx") != -1)
        //    {

        //        music.format = EnmFileFormat.MDX;
        //        uint index = 0;
        //        GD3 gd3 = (new Driver.MXDRV.MXDRV()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackName;
        //        music.titleJ = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".mnd") != -1)
        //    {

        //        music.format = EnmFileFormat.MND;
        //        uint index = 0;
        //        GD3 gd3 = (new Driver.MNDRV.mndrv()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackName;
        //        music.titleJ = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".mub") != -1)
        //    {

        //        music.format = EnmFileFormat.MUB;
        //        uint index = 0;
        //        GD3 gd3 = (new Driver.MUCOM88.MUCOM88()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackName;
        //        music.titleJ = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".muc") != -1)
        //    {

        //        music.format = EnmFileFormat.MUC;
        //        uint index = 0;
        //        GD3 gd3 = (new Driver.MUCOM88.MUCOM88()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackName;
        //        music.titleJ = gd3.TrackName == "" ? Path.GetFileName(file) : gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (file.ToLower().LastIndexOf(".xgm") != -1)
        //    {
        //        music.format = EnmFileFormat.XGM;
        //        GD3 gd3 = new xgm().getGD3Info(buf, 0);
        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //        if (music.title == "" && music.titleJ == "" && music.game == "" && music.gameJ == "" && music.composer == "" && music.composerJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }
        //    }
        //    else if (file.ToLower().LastIndexOf(".s98") != -1)
        //    {
        //        music.format = EnmFileFormat.S98;
        //        GD3 gd3 = new S98().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //    }
        //    else if (file.ToLower().LastIndexOf(".nsf") != -1)
        //    {
        //        nsf nsf = new nsf();
        //        GD3 gd3 = nsf.getGD3Info(buf, 0);

        //        if (gd3 != null)
        //        {
        //            for (int s = 0; s < nsf.songs; s++)
        //            {
        //                music = new PlayList.music();
        //                music.format = EnmFileFormat.NSF;
        //                music.fileName = file;
        //                music.arcFileName = zipFile;
        //                music.arcType = EnmArcType.unknown;
        //                if (!string.IsNullOrEmpty(zipFile)) music.arcType = zipFile.ToLower().LastIndexOf(".zip") != -1 ? EnmArcType.ZIP : EnmArcType.LZH;
        //                music.title = string.Format("{0} - Trk {1}", gd3.GameName, s + 1);
        //                music.titleJ = string.Format("{0} - Trk {1}", gd3.GameNameJ, s + 1);
        //                music.game = gd3.GameName;
        //                music.gameJ = gd3.GameNameJ;
        //                music.composer = gd3.Composer;
        //                music.composerJ = gd3.ComposerJ;
        //                music.vgmby = gd3.VGMBy;
        //                music.converted = gd3.Converted;
        //                music.notes = gd3.Notes;
        //                music.songNo = s;

        //                musics.Add(music);
        //            }

        //            return musics;
        //        }
        //        else
        //        {
        //            music.format = EnmFileFormat.NSF;
        //            music.fileName = file;
        //            music.arcFileName = zipFile;
        //            music.game = "unknown";
        //            music.type = "-";
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //    }
        //    else if (file.ToLower().LastIndexOf(".hes") != -1)
        //    {
        //        hes hes = new hes();
        //        GD3 gd3 = hes.getGD3Info(buf, 0);

        //        for (int s = 0; s < 256; s++)
        //        {
        //            music = new PlayList.music();
        //            music.format = EnmFileFormat.HES;
        //            music.fileName = file;
        //            music.arcFileName = zipFile;
        //            music.arcType = EnmArcType.unknown;
        //            if (!string.IsNullOrEmpty(zipFile)) music.arcType = zipFile.ToLower().LastIndexOf(".zip") != -1 ? EnmArcType.ZIP : EnmArcType.LZH;
        //            music.title = string.Format("{0} - Trk {1}", System.IO.Path.GetFileName(file), s + 1);
        //            music.titleJ = string.Format("{0} - Trk {1}", System.IO.Path.GetFileName(file), s + 1);
        //            music.game = "";
        //            music.gameJ = "";
        //            music.composer = "";
        //            music.composerJ = "";
        //            music.vgmby = "";
        //            music.converted = "";
        //            music.notes = "";
        //            music.songNo = s;

        //            musics.Add(music);
        //        }

        //        return musics;

        //    }
        //    else if (file.ToLower().LastIndexOf(".sid") != -1)
        //    {
        //        Driver.SID.sid sid = new Driver.SID.sid();
        //        GD3 gd3 = sid.getGD3Info(buf, 0);

        //        for (int s = 0; s < sid.songs; s++)
        //        {
        //            music = new PlayList.music();
        //            music.format = EnmFileFormat.SID;
        //            music.fileName = file;
        //            music.arcFileName = zipFile;
        //            music.arcType = EnmArcType.unknown;
        //            if (!string.IsNullOrEmpty(zipFile)) music.arcType = zipFile.ToLower().LastIndexOf(".zip") != -1 ? EnmArcType.ZIP : EnmArcType.LZH;
        //            music.title = string.Format("{0} - Trk {1}", gd3.TrackName, s + 1);
        //            music.titleJ = string.Format("{0} - Trk {1}", gd3.TrackName, s + 1);
        //            music.game = "";
        //            music.gameJ = "";
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.Composer;
        //            music.vgmby = "";
        //            music.converted = "";
        //            music.notes = gd3.Notes;
        //            music.songNo = s;

        //            musics.Add(music);
        //        }

        //        return musics;

        //    }
        //    else if (file.ToLower().LastIndexOf(".mid") != -1)
        //    {
        //        music.format = EnmFileFormat.MID;
        //        GD3 gd3 = new MID().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //        if (music.title == "" && music.titleJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //    }
        //    else if (file.ToLower().LastIndexOf(".rcp") != -1)
        //    {
        //        music.format = EnmFileFormat.RCP;
        //        GD3 gd3 = new RCP().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //        if (music.title == "" && music.titleJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(file));
        //        }

        //    }
        //    else
        //    {
        //        if (buf.Length < 0x40)
        //        {
        //            musics.Add(music);
        //            return musics;
        //        }
        //        if (Common.getLE32(buf, 0x00) != vgm.FCC_VGM)
        //        {
        //            //musics.Add(music);
        //            //return musics;
        //            //VGZかもしれないので確認する
        //            try
        //            {
        //                int num;
        //                buf = new byte[1024]; // 1Kbytesずつ処理する

        //                if (entry == null || entry is ZipArchiveEntry)
        //                {
        //                    Stream inStream; // 入力ストリーム
        //                    if (entry == null)
        //                    {
        //                        inStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        //                    }
        //                    else
        //                    {
        //                        inStream = ((ZipArchiveEntry)entry).Open();
        //                    }
        //                    GZipStream decompStream // 解凍ストリーム
        //                      = new GZipStream(
        //                        inStream, // 入力元となるストリームを指定
        //                        CompressionMode.Decompress); // 解凍（圧縮解除）を指定

        //                    MemoryStream outStream // 出力ストリーム
        //                      = new MemoryStream();

        //                    using (inStream)
        //                    using (outStream)
        //                    using (decompStream)
        //                    {
        //                        while ((num = decompStream.Read(buf, 0, buf.Length)) > 0)
        //                        {
        //                            outStream.Write(buf, 0, num);
        //                        }
        //                    }

        //                    buf = outStream.ToArray();
        //                }
        //                else
        //                {
        //                    UnlhaWrap.UnlhaCmd cmd = new UnlhaWrap.UnlhaCmd();
        //                    buf = cmd.GetFileByte(((Tuple<string, string>)entry).Item1, ((Tuple<string, string>)entry).Item2);
        //                }
        //            }
        //            catch
        //            {
        //                //vgzではなかった
        //            }
        //        }

        //        if (Common.getLE32(buf, 0x00) != vgm.FCC_VGM)
        //        {
        //            musics.Add(music);
        //            return musics;
        //        }

        //        music.format = EnmFileFormat.VGM;
        //        uint version = Common.getLE32(buf, 0x08);
        //        string Version = string.Format("{0}.{1}{2}", (version & 0xf00) / 0x100, (version & 0xf0) / 0x10, (version & 0xf));

        //        uint vgmGd3 = Common.getLE32(buf, 0x14);
        //        GD3 gd3 = new GD3();
        //        if (vgmGd3 != 0)
        //        {
        //            uint vgmGd3Id = Common.getLE32(buf, vgmGd3 + 0x14);
        //            if (vgmGd3Id != vgm.FCC_GD3)
        //            {
        //                musics.Add(music);
        //                return musics;
        //            }
        //            gd3 = (new vgm()).getGD3Info(buf, vgmGd3);
        //        }

        //        uint TotalCounter = Common.getLE32(buf, 0x18);
        //        uint vgmLoopOffset = Common.getLE32(buf, 0x1c);
        //        uint LoopCounter = Common.getLE32(buf, 0x20);

        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //        double sec = (double)TotalCounter / (double)Common.SampleRate;
        //        int TCminutes = (int)(sec / 60);
        //        sec -= TCminutes * 60;
        //        int TCsecond = (int)sec;
        //        sec -= TCsecond;
        //        int TCmillisecond = (int)(sec * 100.0);
        //        music.duration = string.Format("{0:D2}:{1:D2}:{2:D2}", TCminutes, TCsecond, TCmillisecond);
        //    }

        //    musics.Add(music);
        //    return musics;
        //}

        //public static List<PlayList.music> getMusic(PlayList.music ms, byte[] buf, string zipFile = null)
        //{
        //    List<PlayList.music> musics = new List<PlayList.music>();
        //    PlayList.music music = new PlayList.music();

        //    music.format = EnmFileFormat.unknown;
        //    music.fileName = ms.fileName;
        //    music.arcFileName = zipFile;
        //    music.title = "unknown";
        //    music.game = "unknown";
        //    music.type = "-";

        //    if (ms.fileName.ToLower().LastIndexOf(".nrd") != -1)
        //    {

        //        music.format = EnmFileFormat.NRT;
        //        uint index = 42;
        //        GD3 gd3 = (new NRTDRV()).getGD3Info(buf, index);
        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //    }
        //    else if (ms.fileName.ToLower().LastIndexOf(".xgm") != -1)
        //    {
        //        music.format = EnmFileFormat.XGM;
        //        GD3 gd3 = new xgm().getGD3Info(buf, 0);
        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //        if (music.title == "" && music.titleJ == "" && music.game == "" && music.gameJ == "" && music.composer == "" && music.composerJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }
        //    }
        //    else if (ms.fileName.ToLower().LastIndexOf(".s98") != -1)
        //    {
        //        music.format = EnmFileFormat.S98;
        //        GD3 gd3 = new S98().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //    }
        //    else if (ms.fileName.ToLower().LastIndexOf(".nsf") != -1)
        //    {
        //        nsf nsf = new nsf();
        //        GD3 gd3 = nsf.getGD3Info(buf, 0);

        //        if (gd3 != null)
        //        {
        //            if (ms.songNo == -1)
        //            {
        //                for (int s = 0; s < nsf.songs; s++)
        //                {
        //                    music = new PlayList.music();
        //                    music.format = EnmFileFormat.NSF;
        //                    music.fileName = ms.fileName;
        //                    music.arcFileName = zipFile;
        //                    music.title = string.Format("{0} - Trk {1}", gd3.GameName, s);
        //                    music.titleJ = string.Format("{0} - Trk {1}", gd3.GameNameJ, s);
        //                    music.game = gd3.GameName;
        //                    music.gameJ = gd3.GameNameJ;
        //                    music.composer = gd3.Composer;
        //                    music.composerJ = gd3.ComposerJ;
        //                    music.vgmby = gd3.VGMBy;
        //                    music.converted = gd3.Converted;
        //                    music.notes = gd3.Notes;
        //                    music.songNo = s;

        //                    musics.Add(music);
        //                }

        //                return musics;

        //            }
        //            else
        //            {
        //                music.format = EnmFileFormat.NSF;
        //                music.fileName = ms.fileName;
        //                music.arcFileName = zipFile;
        //                music.title = ms.title;
        //                music.titleJ = ms.titleJ;
        //                music.game = gd3.GameName;
        //                music.gameJ = gd3.GameNameJ;
        //                music.composer = gd3.Composer;
        //                music.composerJ = gd3.ComposerJ;
        //                music.vgmby = gd3.VGMBy;
        //                music.converted = gd3.Converted;
        //                music.notes = gd3.Notes;
        //                music.songNo = ms.songNo;
        //            }
        //        }
        //        else
        //        {
        //            music.format = EnmFileFormat.NSF;
        //            music.fileName = ms.fileName;
        //            music.arcFileName = zipFile;
        //            music.game = "unknown";
        //            music.type = "-";
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //    }
        //    else if (ms.fileName.ToLower().LastIndexOf(".mid") != -1)
        //    {
        //        music.format = EnmFileFormat.MID;
        //        GD3 gd3 = new MID().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //        if (music.title == "" && music.titleJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //    }
        //    else if (ms.fileName.ToLower().LastIndexOf(".rcp") != -1)
        //    {
        //        music.format = EnmFileFormat.RCP;
        //        GD3 gd3 = new RCP().getGD3Info(buf, 0);
        //        if (gd3 != null)
        //        {
        //            music.title = gd3.TrackName;
        //            music.titleJ = gd3.TrackNameJ;
        //            music.game = gd3.GameName;
        //            music.gameJ = gd3.GameNameJ;
        //            music.composer = gd3.Composer;
        //            music.composerJ = gd3.ComposerJ;
        //            music.vgmby = gd3.VGMBy;

        //            music.converted = gd3.Converted;
        //            music.notes = gd3.Notes;
        //        }
        //        else
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //        if (music.title == "" && music.titleJ == "")
        //        {
        //            music.title = string.Format("({0})", System.IO.Path.GetFileName(ms.fileName));
        //        }

        //    }
        //    else
        //    {
        //        if (buf.Length < 0x40)
        //        {
        //            musics.Add(music);
        //            return musics;
        //        }
        //        if (Common.getLE32(buf, 0x00) != vgm.FCC_VGM)
        //        {
        //            musics.Add(music);
        //            return musics;
        //        }

        //        music.format = EnmFileFormat.VGM;
        //        uint version = Common.getLE32(buf, 0x08);
        //        string Version = string.Format("{0}.{1}{2}", (version & 0xf00) / 0x100, (version & 0xf0) / 0x10, (version & 0xf));

        //        uint vgmGd3 = Common.getLE32(buf, 0x14);
        //        GD3 gd3 = new GD3();
        //        if (vgmGd3 != 0)
        //        {
        //            uint vgmGd3Id = Common.getLE32(buf, vgmGd3 + 0x14);
        //            if (vgmGd3Id != vgm.FCC_GD3)
        //            {
        //                musics.Add(music);
        //                return musics;
        //            }
        //            gd3 = (new vgm()).getGD3Info(buf, vgmGd3);
        //        }

        //        uint TotalCounter = Common.getLE32(buf, 0x18);
        //        uint vgmLoopOffset = Common.getLE32(buf, 0x1c);
        //        uint LoopCounter = Common.getLE32(buf, 0x20);

        //        music.title = gd3.TrackName;
        //        music.titleJ = gd3.TrackNameJ;
        //        music.game = gd3.GameName;
        //        music.gameJ = gd3.GameNameJ;
        //        music.composer = gd3.Composer;
        //        music.composerJ = gd3.ComposerJ;
        //        music.vgmby = gd3.VGMBy;

        //        music.converted = gd3.Converted;
        //        music.notes = gd3.Notes;

        //        double sec = (double)TotalCounter / (double)Common.SampleRate;
        //        int TCminutes = (int)(sec / 60);
        //        sec -= TCminutes * 60;
        //        int TCsecond = (int)sec;
        //        sec -= TCsecond;
        //        int TCmillisecond = (int)(sec * 100.0);
        //        music.duration = string.Format("{0:D2}:{1:D2}:{2:D2}", TCminutes, TCsecond, TCmillisecond);
        //    }

        //    musics.Add(music);
        //    return musics;
        //}

        public static void RealChipClose()
        {
            if (realChip != null)
            {
                realChip.Close();
            }
        }

        public static List<Setting.ChipType> GetRealChipList(EnmRealChipType scciType)
        {
            if (realChip == null) return null;
            return realChip.GetRealChipList(scciType);
        }

        //private static void MakeMIDIout(Setting setting, int m)
        //{
        //    if (setting.midiOut.lstMidiOutInfo == null || setting.midiOut.lstMidiOutInfo.Count < 1) return;
        //    if (setting.midiOut.lstMidiOutInfo[m] == null || setting.midiOut.lstMidiOutInfo[m].Length < 1) return;

        //    for (int i = 0; i < setting.midiOut.lstMidiOutInfo[m].Length; i++)
        //    {
        //        int n = -1;
        //        int t = 0;
        //        NAudio.Midi.MidiOut mo = null;
        //        int vn = -1;
        //        int vt = 0;
        //        vstInfo2 vmo = null;

        //        for (int j = 0; j < NAudio.Midi.MidiOut.NumberOfDevices; j++)
        //        {
        //            if (setting.midiOut.lstMidiOutInfo[m][i].name != NAudio.Midi.MidiOut.DeviceInfo(j).ProductName) continue;

        //            n = j;
        //            t = setting.midiOut.lstMidiOutInfo[m][i].type;
        //            break;
        //        }

        //        if (n != -1)
        //        {
        //            try
        //            {
        //                mo = new NAudio.Midi.MidiOut(n);
        //            }
        //            catch
        //            {
        //                mo = null;
        //            }
        //        }


        //        if (n == -1)
        //        {
        //            for (int j = 0; j < vstPluginsInst.Count; j++)
        //            {
        //                if (!vstPluginsInst[j].isInstrument || setting.midiOut.lstMidiOutInfo[m][i].fileName != vstPluginsInst[j].fileName) continue;
        //                bool k = false;
        //                foreach (vstInfo2 v in vstMidiOuts) if (v == vstPluginsInst[j]) { k = true; break; }
        //                if (k) continue;
        //                vn = j;
        //                vt = setting.midiOut.lstMidiOutInfo[m][i].type;
        //                break;
        //            }

        //            if (vn != -1)
        //            {
        //                try
        //                {
        //                    vmo = vstPluginsInst[vn];
        //                }
        //                catch
        //                {
        //                    vmo = null;
        //                }
        //            }
        //        }

        //        if (mo != null || vmo != null)
        //        {
        //            midiOuts.Add(mo);
        //            midiOutsType.Add(t);

        //            vstMidiOuts.Add(vmo);
        //            vstMidiOutsType.Add(vt);
        //        }
        //    }
        //}

        //private static void ReleaseAllMIDIout()
        //{
        //    if (midiOuts.Count > 0)
        //    {
        //        for (int i = 0; i < midiOuts.Count; i++)
        //        {
        //            if (midiOuts[i] != null)
        //            {
        //                try
        //                {
        //                    //resetできない機種もある?
        //                    midiOuts[i].Reset();
        //                }
        //                catch { }
        //                midiOuts[i].Close();
        //                midiOuts[i] = null;
        //            }
        //        }
        //        midiOuts.Clear();
        //        midiOutsType.Clear();
        //    }

        //    if (vstMidiOuts.Count > 0)
        //    {
        //        vstMidiOuts.Clear();
        //        vstMidiOutsType.Clear();
        //    }
        //}

        public static MDSound.MDSound.Chip GetMDSChipInfo(MDSound.MDSound.enmInstrumentType typ)
        {
            return chipRegister.GetChipInfo(typ);
        }

        public static int getLatency()
        {
            if (setting.outputDevice.DeviceType != Common.DEV_AsioOut)
            {
                return (int)Common.SampleRate * setting.outputDevice.Latency / 1000;
            }
            return naudioWrap.getAsioLatency();
        }

        //public static void SetVGMBuffer(EnmFileFormat format, byte[] srcBuf, string playingFileName, string playingArcFileName, int midiMode, int songNo, List<Tuple<string, byte[]>> extFile)
        //{
        //    //Stop();
        //    PlayingFileFormat = format;
        //    vgmBuf = srcBuf;
        //    PlayingFileName = playingFileName;//WaveWriter向け
        //    PlayingArcFileName = playingArcFileName;
        //    MidiMode = midiMode;
        //    SongNo = songNo;
        //    //chipRegister.SetFileName(playingFileName);//ExportMIDI向け
        //    ExtendFile = extFile;//追加ファイル
        //}

        //public static void getPlayingFileName(out string playingFileName, out string playingArcFileName)
        //{
        //    playingFileName = PlayingFileName;
        //    playingArcFileName = PlayingArcFileName;
        //}



        public static bool Play(Setting setting)
        {
            bool ret = false;

            useEmu = false;
            useReal = false;

            errMsg = "";
            Stop();

            sm.SetSpeed(1.0);
            vgmSpeed = 1.0;

            //スレッドなどの準備など(?)で演奏開始時にテンポが乱れることがあるため念のため待つ。
            DriverSeqCounter = sm.GetDriverSeqCounterDelay();

            //開始時にバッファ分のデータが貯まらないうちにコールバックがくるとテンポが乱れるため、レイテンシ(デバイスのバッファ)分だけ演奏開始を待つ。
            DriverSeqCounter += getLatency();

            log.Write(string.Format("Playing filename [{0}]", PlayingFileName));

            //try
            //{
            //    waveWriter.Open(PlayingFileName);
            //}
            //catch
            //{
            //    errMsg = "wave file open error.";
            //    return false;
            //}

            MDSound.MDSound.np_nes_apu_volume = 0;
            MDSound.MDSound.np_nes_dmc_volume = 0;
            MDSound.MDSound.np_nes_fds_volume = 0;
            MDSound.MDSound.np_nes_fme7_volume = 0;
            MDSound.MDSound.np_nes_mmc5_volume = 0;
            MDSound.MDSound.np_nes_n106_volume = 0;
            MDSound.MDSound.np_nes_vrc6_volume = 0;
            MDSound.MDSound.np_nes_vrc7_volume = 0;


            //if (PlayingFileFormat == EnmFileFormat.MUC)
            //{
            //    driver = new Driver.MUCOM88.MUCOM88();
            //    driver.setting = setting;
            //    ((Driver.MUCOM88.MUCOM88)driver).PlayingFileName = PlayingFileName;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new Driver.MUCOM88.MUCOM88();
            //    //    driverReal.setting = setting;
            //    //    ((Driver.MUCOM88.MUCOM88)driverReal).PlayingFileName = PlayingFileName;
            //    //}
            //    return mucPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.NRT)
            //{
            //    driver = new NRTDRV();
            //    driver.setting = setting;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new NRTDRV();
            //    //    driverReal.setting = setting;
            //    //}
            //    return nrdPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.MDR)
            //{
            //    driver = new Driver.MoonDriver.MoonDriver();
            //    driver.setting = setting;
            //    ((Driver.MoonDriver.MoonDriver)driver).ExtendFile = (ExtendFile != null && ExtendFile.Count > 0) ? ExtendFile[0] : null;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new Driver.MoonDriver.MoonDriver();
            //    //    driverReal.setting = setting;
            //    //    ((Driver.MoonDriver.MoonDriver)driverReal).ExtendFile = (ExtendFile != null && ExtendFile.Count > 0) ? ExtendFile[0] : null;
            //    //}
            //    return mdrPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.MDX)
            //{
            //    driver = new Driver.MXDRV.MXDRV();
            //    driver.setting = setting;
            //    ((Driver.MXDRV.MXDRV)driver).ExtendFile = (ExtendFile != null && ExtendFile.Count > 0) ? ExtendFile[0] : null;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new Driver.MXDRV.MXDRV();
            //    //    driverReal.setting = setting;
            //    //    ((Driver.MXDRV.MXDRV)driverReal).ExtendFile = (ExtendFile != null && ExtendFile.Count > 0) ? ExtendFile[0] : null;
            //    //}
            //    return mdxPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.MND)
            //{
            //    driver = new Driver.MNDRV.mndrv();
            //    driver.setting = setting;
            //    ((Driver.MNDRV.mndrv)driver).ExtendFile = ExtendFile;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new Driver.MNDRV.mndrv();
            //    //    driverReal.setting = setting;
            //    //    ((Driver.MNDRV.mndrv)driverReal).ExtendFile = ExtendFile;
            //    //}
            //    return mndPlay(setting);
            //}

            if (PlayingFileFormat == EnmFileFormat.XGM)
            {
                driver = new xgm();
                driver.setting = setting;
                //driverReal = null;
                //if (setting.outputDevice.DeviceType != Common.DEV_Null)
                //{
                //    driverReal = new xgm();
                //    driverReal.setting = setting;
                //}

                return xgmPlay(setting);
            }

            //if (PlayingFileFormat == EnmFileFormat.S98)
            //{
            //    driver = new S98();
            //    driver.setting = setting;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new S98();
            //    //    driverReal.setting = setting;
            //    //}

            //    return s98Play(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.MID)
            //{
            //    driver = new MID();
            //    driver.setting = setting;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new MID();
            //    //    driverReal.setting = setting;
            //    //}
            //    ret = midPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.RCP)
            //{
            //    driver = new RCP();
            //    driver.setting = setting;
            //    ((RCP)driver).ExtendFile = ExtendFile;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new RCP();
            //    //    driverReal.setting = setting;
            //    //    ((RCP)driverReal).ExtendFile = ExtendFile;
            //    //}
            //    ret = RcpPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.NSF)
            //{
            //    driver = new nsf();
            //    driver.setting = setting;
            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new nsf();
            //    //    driverReal.setting = setting;
            //    //}
            //    return nsfPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.HES)
            //{
            //    driver = new hes();
            //    driver.setting = setting;

            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new hes();
            //    //    driverReal.setting = setting;
            //    //}
            //    return hesPlay(setting);
            //}

            //if (PlayingFileFormat == EnmFileFormat.SID)
            //{
            //    driver = new Driver.SID.sid();
            //    driver.setting = setting;

            //    //driverReal = null;
            //    //if (setting.outputDevice.DeviceType != Common.DEV_Null)
            //    //{
            //    //    driverReal = new Driver.SID.sid();
            //    //    driverReal.setting = setting;
            //    //}
            //    return sidPlay(setting);
            //}

            if (PlayingFileFormat == EnmFileFormat.VGM)
            {
                driver = new vgm();
                driver.setting = setting;
                ((vgm)driver).dacControl.chipRegister = chipRegister;
                ((vgm)driver).dacControl.model = EnmModel.VirtualModel;


                //driverReal = null;
                //if (setting.outputDevice.DeviceType != Common.DEV_Null)
                //{
                //    driverReal = new vgm();
                //    driverReal.setting = setting;
                //    ((vgm)driverReal).dacControl.chipRegister = chipRegister;
                //    ((vgm)driverReal).dacControl.model = EnmModel.RealModel;
                //}
                ret= vgmPlay(setting);
            }

            if (!ret) return false;

            sm.RequestStart();
            while (!sm.IsRunningAsync())
            {
            }

            EmuSeqCounter = 0;
            Stopped = false;

            if(!useEmu) sm.RequestStopAtEmuChipSender();
            if(!useReal) sm.RequestStopAtRealChipSender();

            //if (rsc == null)
            //{
            //    sm.RequestStopAtRealChipSender();
            //    while (sm.IsRunningAtRealChipSender()) ;
            //}
            //else
            //{
            //    sm.RequestStopAtEmuChipSender();
            //    while (sm.IsRunningAtEmuChipSender()) ;
            //}

            return ret;
        }

        //public static bool mucPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();
        //        MDSound.MDSound.Chip chip;

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        MasterVolume = setting.balance.MasterVolume;

        //        ym2608 ym2608 = null;
        //        chip = new MDSound.MDSound.Chip();
        //        ym2608 = new ym2608();
        //        chip.ID = 0;
        //        chipLED.PriOPNA = 1;
        //        chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
        //        chip.Instrument = ym2608;
        //        chip.Update = ym2608.Update;
        //        chip.Start = ym2608.Start;
        //        chip.Stop = ym2608.Stop;
        //        chip.Reset = ym2608.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Volume = setting.balance.YM2608Volume;
        //        chip.Clock = Driver.MUCOM88.MUCOM88.baseclock;
        //        chip.Option = new object[] { Common.GetApplicationFolder() };
        //        //hiyorimiDeviceFlag |= 0x2;
        //        lstChips.Add(chip);
        //        useChip.Add(EnmChip.YM2608);

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        SetYM2608Volume(true, setting.balance.YM2608Volume);
        //        SetYM2608FMVolume(true, setting.balance.YM2608FMVolume);
        //        SetYM2608PSGVolume(true, setting.balance.YM2608PSGVolume);
        //        SetYM2608RhythmVolume(true, setting.balance.YM2608RhythmVolume);
        //        SetYM2608AdpcmVolume(true, setting.balance.YM2608AdpcmVolume);

        //        chipRegister.YM2608SetRegister(0, 0, 0, 0x2d, 0x00);
        //        chipRegister.YM2608SetRegister(0, 0, 0, 0x29, 0x82);
        //        chipRegister.YM2608SetRegister(0, 1, 0, 0x29, 0x82);
        //        chipRegister.YM2608SetRegister(0, 0, 0, 0x07, 0x38); //PSG TONE でリセット

        //        chipRegister.YM2608WriteClock(0, Driver.MUCOM88.MUCOM88.baseclock);
        //        chipRegister.YM2608WriteClock(1, Driver.MUCOM88.MUCOM88.baseclock);
        //        //chipRegister.setYM2203SSGVolume(0, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2203SSGVolume(1, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        chipRegister.YM2608SetSSGVolume(0, setting.balance.GimicOPNAVolume);
        //        chipRegister.YM2608SetSSGVolume(1, setting.balance.GimicOPNAVolume);


        //        if (!driver.init(vgmBuf, chipRegister, new EnmChip[] { EnmChip.YM2608 }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.YM2608 }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        //Play

        //        Paused = false;

        //        //if (driverReal != null && setting.YM2608Type.UseScci)
        //        //{
        //        //    realChip.WaitOPNADPCMData(setting.YM2608Type.SoundLocation == -1);
        //        //}

        //        Stopped = false;
        //        //oneTimeReset = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool nrdPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        int r = ((NRTDRV)driver).checkUseChip(vgmBuf);

        //        chipRegister.YM2151SetFadeoutVolume(0, 0);
        //        chipRegister.YM2151SetFadeoutVolume(1, 0);

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        MDSound.MDSound.Chip chip;

        //        hiyorimiNecessary = setting.HiyorimiMode;
        //        int hiyorimiDeviceFlag = 0;

        //        chipLED = new ChipLEDs();

        //        MasterVolume = setting.balance.MasterVolume;

        //        MDSound.ym2151 ym2151 = null;
        //        MDSound.ym2151_mame ym2151_mame = null;
        //        MDSound.ym2151_x68sound ym2151_x68sound = null;
        //        for (int i = 0; i < 2; i++)
        //        {
        //            if ((i == 0 && (r & 0x3) != 0) || (i == 1 && (r & 0x2) != 0))
        //            {
        //                chip = new MDSound.MDSound.Chip();
        //                chip.ID = (byte)i;

        //                if ((i == 0 && setting.YM2151Type.UseEmu) || (i == 1 && setting.YM2151SType.UseEmu))
        //                {
        //                    if (ym2151 == null) ym2151 = new MDSound.ym2151();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
        //                    chip.Instrument = ym2151;
        //                    chip.Update = ym2151.Update;
        //                    chip.Start = ym2151.Start;
        //                    chip.Stop = ym2151.Stop;
        //                    chip.Reset = ym2151.Reset;
        //                }
        //                else if ((i == 0 && setting.YM2151Type.UseEmu2) || (i == 1 && setting.YM2151SType.UseEmu2))
        //                {
        //                    if (ym2151_mame == null) ym2151_mame = new MDSound.ym2151_mame();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2151mame;
        //                    chip.Instrument = ym2151_mame;
        //                    chip.Update = ym2151_mame.Update;
        //                    chip.Start = ym2151_mame.Start;
        //                    chip.Stop = ym2151_mame.Stop;
        //                    chip.Reset = ym2151_mame.Reset;
        //                }
        //                else if ((i == 0 && setting.YM2151Type.UseEmu3) || (i == 1 && setting.YM2151SType.UseEmu3))
        //                {
        //                    if (ym2151_x68sound == null) ym2151_x68sound = new MDSound.ym2151_x68sound();
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2151x68sound;
        //                    chip.Instrument = ym2151_x68sound;
        //                    chip.Update = ym2151_x68sound.Update;
        //                    chip.Start = ym2151_x68sound.Start;
        //                    chip.Stop = ym2151_x68sound.Stop;
        //                    chip.Reset = ym2151_x68sound.Reset;
        //                }

        //                chip.SamplingRate = (UInt32)Common.SampleRate;
        //                chip.Volume = setting.balance.YM2151Volume;
        //                chip.Clock = 4000000;
        //                chip.Option = null;

        //                hiyorimiDeviceFlag |= 0x2;

        //                if (i == 0) chipLED.PriOPM = 1;
        //                else chipLED.SecOPM = 1;

        //                if (chip.Start != null)
        //                {
        //                    lstChips.Add(chip);
        //                    useChip.Add(i == 0 ? EnmChip.YM2151 : EnmChip.S_YM2151);
        //                }
        //            }
        //        }

        //        if ((r & 0x4) != 0)
        //        {
        //            MDSound.ay8910 ay8910 = new MDSound.ay8910();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
        //            chip.ID = (byte)0;
        //            chip.Instrument = ay8910;
        //            chip.Update = ay8910.Update;
        //            chip.Start = ay8910.Start;
        //            chip.Stop = ay8910.Stop;
        //            chip.Reset = ay8910.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.AY8910Volume;
        //            chip.Clock = 2000000 / 2;
        //            clockAY8910 = (int)chip.Clock;
        //            chip.Option = null;

        //            hiyorimiDeviceFlag |= 0x1;
        //            chipLED.PriAY10 = 1;

        //            lstChips.Add(chip);
        //            useChip.Add(EnmChip.AY8910);
        //        }

        //        if (hiyorimiDeviceFlag == 0x3 && hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        if (useChip.Contains(EnmChip.YM2151) || useChip.Contains(EnmChip.S_YM2151))
        //            SetYM2151Volume(false, setting.balance.YM2151Volume);
        //        if (useChip.Contains(EnmChip.AY8910))
        //            SetAY8910Volume(false, setting.balance.AY8910Volume);

        //        if (useChip.Contains(EnmChip.YM2151))
        //            chipRegister.YM2151WriteClock(0, 4000000);
        //        if (useChip.Contains(EnmChip.S_YM2151))
        //            chipRegister.YM2151WriteClock(1, 4000000);

        //        //driver.SetYM2151Hosei(4000000);
        //        //driverReal.SetYM2151Hosei(4000000);
        //        //chipRegister.setYM2203SSGVolume(0, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2203SSGVolume(1, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(0, setting.balance.GimicOPNAVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(1, setting.balance.GimicOPNAVolume, enmModel.RealModel);


        //        driver.init(vgmBuf, chipRegister, new EnmChip[] { EnmChip.YM2151, EnmChip.AY8910 }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000));
        //        ((NRTDRV)driver).Call(0);//

        //        //if (driverReal != null)
        //        //{
        //        //    driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.YM2151, EnmChip.AY8910 }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000));
        //        //    ((NRTDRV)driverReal).Call(0);//
        //        //}


        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        ((NRTDRV)driver).Call(1);//MPLAY

        //        //if (driverReal != null)
        //        //{
        //        //    ((NRTDRV)driverReal).Call(1);//MPLAY
        //        //}



        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool mdrPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //int r = ((NRTDRV)driverVirtual).checkUseChip(vgmBuf);

        //        chipRegister.YM2151SetFadeoutVolume(0, 0);
        //        chipRegister.YM2151SetFadeoutVolume(1, 0);

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        MDSound.MDSound.Chip chip;

        //        hiyorimiNecessary = setting.HiyorimiMode;
        //        int hiyorimiDeviceFlag = 0;

        //        chipLED = new ChipLEDs();

        //        MasterVolume = setting.balance.MasterVolume;

        //        MDSound.ymf278b ymf278b = new MDSound.ymf278b();

        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
        //        chip.ID = 0;
        //        chip.Instrument = ymf278b;
        //        chip.Update = ymf278b.Update;
        //        chip.Start = ymf278b.Start;
        //        chip.Stop = ymf278b.Stop;
        //        chip.Reset = ymf278b.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Volume = setting.balance.YMF278BVolume;
        //        chip.Clock = 33868800;// 4000000;
        //        chip.Option = new object[] { Common.GetApplicationFolder() };

        //        hiyorimiDeviceFlag |= 0x2;

        //        chipLED.PriOPL4 = 1;

        //        lstChips.Add(chip);
        //        useChip.Add(EnmChip.YMF278B);

        //        if (hiyorimiDeviceFlag == 0x3 && hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        SetYMF278BVolume(true, setting.balance.YMF278BVolume);
        //        //chipRegister.setYM2203SSGVolume(0, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2203SSGVolume(1, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(0, setting.balance.GimicOPNAVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(1, setting.balance.GimicOPNAVolume, enmModel.RealModel);

        //        driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000));
        //        //if (driverReal != null)
        //        //{
        //        //    driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000));
        //        //}

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool mdxPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        hiyorimiNecessary = setting.HiyorimiMode;
        //        int hiyorimiDeviceFlag = 3;

        //        chipLED = new ChipLEDs();

        //        MasterVolume = setting.balance.MasterVolume;

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();
        //        MDSound.MDSound.Chip chip = null;

        //        if (setting.YM2151Type.UseEmu)
        //        {
        //            MDSound.ym2151 ym2151 = new MDSound.ym2151();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
        //            chip.ID = (byte)0;
        //            chip.Instrument = ym2151;
        //            chip.Update = ym2151.Update;
        //            chip.Start = ym2151.Start;
        //            chip.Stop = ym2151.Stop;
        //            chip.Reset = ym2151.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = null;
        //        }
        //        else if (setting.YM2151Type.UseEmu2)
        //        {
        //            MDSound.ym2151_mame ym2151mame = new MDSound.ym2151_mame();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151mame;
        //            chip.ID = (byte)0;
        //            chip.Instrument = ym2151mame;
        //            chip.Update = ym2151mame.Update;
        //            chip.Start = ym2151mame.Start;
        //            chip.Stop = ym2151mame.Stop;
        //            chip.Reset = ym2151mame.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = null;
        //        }
        //        else if (setting.YM2151Type.UseEmu3)
        //        {
        //            MDSound.ym2151_x68sound mdxOPM = new MDSound.ym2151_x68sound();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151x68sound;
        //            chip.ID = (byte)0;
        //            chip.Instrument = mdxOPM;
        //            chip.Update = mdxOPM.Update;
        //            chip.Start = mdxOPM.Start;
        //            chip.Stop = mdxOPM.Stop;
        //            chip.Reset = mdxOPM.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = new object[3] { 1, 0, 0 };
        //        }
        //        if (chip != null)
        //        {
        //            lstChips.Add(chip);
        //        }
        //        useChip.Add(EnmChip.YM2151);

        //        MDSound.ym2151_x68sound mdxPCM_V = new MDSound.ym2151_x68sound();
        //        mdxPCM_V.x68sound[0] = new MDSound.NX68Sound.X68Sound();
        //        mdxPCM_V.sound_Iocs[0] = new MDSound.NX68Sound.sound_iocs(mdxPCM_V.x68sound[0]);
        //        MDSound.ym2151_x68sound mdxPCM_R = new MDSound.ym2151_x68sound();
        //        mdxPCM_R.x68sound[0] = new MDSound.NX68Sound.X68Sound();
        //        mdxPCM_R.sound_Iocs[0] = new MDSound.NX68Sound.sound_iocs(mdxPCM_R.x68sound[0]);
        //        useChip.Add(EnmChip.OKIM6258);

        //        chipLED.PriOPM = 1;
        //        chipLED.PriOKI5 = 1;


        //        if (hiyorimiDeviceFlag == 0x3 && hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        SetYM2151Volume(false, setting.balance.YM2151Volume);

        //        if (useChip.Contains(EnmChip.YM2151))
        //            chipRegister.YM2151WriteClock(0, 4000000);
        //        //chipRegister.writeYM2151Clock(1, 4000000, enmModel.RealModel);

        //        //driver.SetYM2151Hosei(4000000);
        //        //chipRegister.setYM2203SSGVolume(0, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2203SSGVolume(1, setting.balance.GimicOPNVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(0, setting.balance.GimicOPNAVolume, enmModel.RealModel);
        //        //chipRegister.setYM2608SSGVolume(1, setting.balance.GimicOPNAVolume, enmModel.RealModel);

        //        bool retV = ((MDPlayer.Driver.MXDRV.MXDRV)driver).init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)
        //            , mdxPCM_V);
        //        //bool retR = true;
        //        //if (driverReal != null)
        //        //{
        //        //    retR = ((MDPlayer.Driver.MXDRV.MXDRV)driverReal).init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)
        //        //        , mdxPCM_R);
        //        //}

        //        if (!retV)// || !retR)
        //        {
        //            errMsg = driver.errMsg;// != "" ? driverVirtual.errMsg : driverReal.errMsg;
        //            return false;
        //        }

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool mndPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        hiyorimiNecessary = setting.HiyorimiMode;
        //        int hiyorimiDeviceFlag = 3;

        //        chipLED = new ChipLEDs();

        //        MasterVolume = setting.balance.MasterVolume;

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();
        //        MDSound.MDSound.Chip chip = null;

        //        if (setting.YM2151Type.UseEmu)
        //        {
        //            MDSound.ym2151 ym2151 = new MDSound.ym2151();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
        //            chip.ID = (byte)0;
        //            chip.Instrument = ym2151;
        //            chip.Update = ym2151.Update;
        //            chip.Start = ym2151.Start;
        //            chip.Stop = ym2151.Stop;
        //            chip.Reset = ym2151.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = null;
        //        }
        //        else if (setting.YM2151Type.UseEmu2)
        //        {
        //            MDSound.ym2151_mame ym2151mame = new MDSound.ym2151_mame();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151mame;
        //            chip.ID = (byte)0;
        //            chip.Instrument = ym2151mame;
        //            chip.Update = ym2151mame.Update;
        //            chip.Start = ym2151mame.Start;
        //            chip.Stop = ym2151mame.Stop;
        //            chip.Reset = ym2151mame.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = null;
        //        }
        //        else if (setting.YM2151Type.UseEmu3)
        //        {
        //            MDSound.ym2151_x68sound mdxOPM = new MDSound.ym2151_x68sound();
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2151x68sound;
        //            chip.ID = (byte)0;
        //            chip.Instrument = mdxOPM;
        //            chip.Update = mdxOPM.Update;
        //            chip.Start = mdxOPM.Start;
        //            chip.Stop = mdxOPM.Stop;
        //            chip.Reset = mdxOPM.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2151Volume;
        //            chip.Clock = 4000000;
        //            chip.Option = new object[3] { 1, 0, 0 };
        //        }
        //        if (chip != null)
        //        {
        //            lstChips.Add(chip);
        //        }
        //        useChip.Add(EnmChip.YM2151);

        //        MDSound.ym2608 opna = new ym2608();
        //        if (setting.YM2608Type.UseEmu)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
        //            chip.ID = (byte)0;
        //            chip.Instrument = opna;
        //            chip.Update = opna.Update;
        //            chip.Start = opna.Start;
        //            chip.Stop = opna.Stop;
        //            chip.Reset = opna.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2608Volume;
        //            chip.Clock = 8000000;// 7987200;
        //            chip.Option = new object[] { Common.GetApplicationFolder() };
        //            lstChips.Add(chip);
        //        }
        //        useChip.Add(EnmChip.YM2608);

        //        if (setting.YM2608SType.UseEmu)
        //        {
        //            chip = new MDSound.MDSound.Chip();
        //            chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
        //            chip.ID = (byte)1;
        //            chip.Instrument = opna;
        //            chip.Update = opna.Update;
        //            chip.Start = opna.Start;
        //            chip.Stop = opna.Stop;
        //            chip.Reset = opna.Reset;
        //            chip.SamplingRate = (UInt32)Common.SampleRate;
        //            chip.Volume = setting.balance.YM2608Volume;
        //            chip.Clock = 8000000;// 7987200;
        //            chip.Option = new object[] { Common.GetApplicationFolder() };
        //            lstChips.Add(chip);
        //        }
        //        useChip.Add(EnmChip.S_YM2608);

        //        MDSound.mpcmX68k mpcm = new mpcmX68k();
        //        chip = new MDSound.MDSound.Chip();
        //        chip.type = MDSound.MDSound.enmInstrumentType.mpcmX68k;
        //        chip.ID = (byte)0;
        //        chip.Instrument = mpcm;
        //        chip.Update = mpcm.Update;
        //        chip.Start = mpcm.Start;
        //        chip.Stop = mpcm.Stop;
        //        chip.Reset = mpcm.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Volume = setting.balance.OKIM6258Volume;
        //        chip.Clock = 15600;
        //        chip.Option = new object[] { Common.GetApplicationFolder() };
        //        lstChips.Add(chip);
        //        useChip.Add(EnmChip.OKIM6258);

        //        chipLED.PriOPM = 1;
        //        chipLED.PriOPNA = 1;
        //        chipLED.SecOPNA = 1;
        //        chipLED.PriOKI5 = 1;

        //        if (hiyorimiDeviceFlag == 0x3 && hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        if (useChip.Contains(EnmChip.YM2151) || useChip.Contains(EnmChip.S_YM2151))
        //            SetYM2151Volume(false, setting.balance.YM2151Volume);

        //        if (useChip.Contains(EnmChip.YM2608) || useChip.Contains(EnmChip.S_YM2608))
        //        {
        //            SetYM2608Volume(true, setting.balance.YM2608Volume);
        //            SetYM2608FMVolume(true, setting.balance.YM2608FMVolume);
        //            SetYM2608PSGVolume(true, setting.balance.YM2608PSGVolume);
        //            SetYM2608RhythmVolume(true, setting.balance.YM2608RhythmVolume);
        //            SetYM2608AdpcmVolume(true, setting.balance.YM2608AdpcmVolume);
        //        }

        //        Thread.Sleep(500);

        //        if (useChip.Contains(EnmChip.YM2608))
        //        {
        //            chipRegister.YM2608SetRegister(0, 0, 0, 0x2d, 0x00);
        //            chipRegister.YM2608SetRegister(0, 0, 0, 0x29, 0x82);
        //            chipRegister.YM2608SetRegister(0, 0, 0, 0x07, 0x38); //PSG TONE でリセット
        //            chipRegister.YM2608WriteClock(0, 8000000);
        //            chipRegister.YM2608SetSSGVolume(0, setting.balance.GimicOPNAVolume);
        //        }

        //        if (useChip.Contains(EnmChip.S_YM2608))
        //        {
        //            chipRegister.YM2608SetRegister(0, 1, 0, 0x2d, 0x00);
        //            chipRegister.YM2608SetRegister(0, 1, 0, 0x29, 0x82);
        //            chipRegister.YM2608SetRegister(0, 1, 0, 0x07, 0x38); //PSG TONE でリセット
        //            chipRegister.YM2608WriteClock(1, 8000000);
        //            chipRegister.YM2608SetSSGVolume(1, setting.balance.GimicOPNAVolume);
        //        }

        //        if (useChip.Contains(EnmChip.YM2151))
        //            chipRegister.YM2151WriteClock(0, 4000000);
        //        if (useChip.Contains(EnmChip.S_YM2151))
        //            chipRegister.YM2151WriteClock(1, 4000000);

        //        //driver.SetYM2151Hosei(4000000);

        //        if (useChip.Contains(EnmChip.YM2203))
        //            chipRegister.YM2203SetSSGVolume(0, setting.balance.GimicOPNVolume);
        //        if (useChip.Contains(EnmChip.S_YM2203))
        //            chipRegister.YM2203SetSSGVolume(1, setting.balance.GimicOPNVolume);

        //        bool retV = ((MDPlayer.Driver.MNDRV.mndrv)driver).init(vgmBuf, chipRegister, new EnmChip[] { EnmChip.YM2151, EnmChip.YM2608 }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)
        //            );
        //        //bool retR = true;
        //        //if (driverReal != null)
        //        //{
        //        //    retR = ((MDPlayer.Driver.MNDRV.mndrv)driverReal).init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.YM2151, EnmChip.YM2608 }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)
        //        //        );
        //        //}

        //        if (!retV)// || !retR)
        //        {
        //            errMsg = driver.errMsg;// != "" ? driverVirtual.errMsg : driverReal.errMsg;
        //            return false;
        //        }

        //        ((MDPlayer.Driver.MNDRV.mndrv)driver).m_MPCM = mpcm;

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        public static bool xgmPlay(Setting setting)
        {

            try
            {

                if (vgmBuf == null || setting == null) return false;

                //Stop();

                //chipRegister.resetChips();
                ResetFadeOutParam();
                useChip.Clear();

                //startTrdVgmReal();

                List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

                MDSound.MDSound.Chip chip;

                hiyorimiNecessary = setting.HiyorimiMode;

                chipLED = new ChipLEDs();

                MasterVolume = setting.balance.MasterVolume;

                chip = new MDSound.MDSound.Chip();
                chip.ID = (byte)0;
                MDSound.ym2612 ym2612 = null;
                MDSound.ym3438 ym3438 = null;

                if (setting.YM2612Type.UseEmu)
                {
                    if (ym2612 == null) ym2612 = new ym2612();
                    chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
                    chip.Instrument = ym2612;
                    chip.Update = ym2612.Update;
                    chip.Start = ym2612.Start;
                    chip.Stop = ym2612.Stop;
                    chip.Reset = ym2612.Reset;
                }
                else if (setting.YM2612Type.UseEmu2)
                {
                    if (ym3438 == null) ym3438 = new ym3438();
                    chip.type = MDSound.MDSound.enmInstrumentType.YM3438;
                    chip.Instrument = ym3438;
                    chip.Update = ym3438.Update;
                    chip.Start = ym3438.Start;
                    chip.Stop = ym3438.Stop;
                    chip.Reset = ym3438.Reset;
                    switch (setting.nukedOPN2.EmuType)
                    {
                        case 0:
                            ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.discrete);
                            break;
                        case 1:
                            ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic);
                            break;
                        case 2:
                            ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612);
                            break;
                        case 3:
                            ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612_u);
                            break;
                        case 4:
                            ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic_lp);
                            break;
                    }
                }
                chip.SamplingRate = (UInt32)Common.SampleRate;
                chip.Volume = setting.balance.YM2612Volume;
                chip.Clock = 7670454;
                chip.Option = null;
                chipLED.PriOPN2 = 1;
                lstChips.Add(chip);
                useChip.Add(EnmChip.YM2612);

                sn76489 sn76489 = new sn76489();
                chip = new MDSound.MDSound.Chip();
                chip.type = MDSound.MDSound.enmInstrumentType.SN76489;
                chip.ID = (byte)0;
                chip.Instrument = sn76489;
                chip.Update = sn76489.Update;
                chip.Start = sn76489.Start;
                chip.Stop = sn76489.Stop;
                chip.Reset = sn76489.Reset;
                chip.SamplingRate = (UInt32)Common.SampleRate;
                chip.Volume = setting.balance.SN76489Volume;
                chip.Clock = 3579545;
                chip.Option = null;
                chipLED.PriDCSG = 1;
                lstChips.Add(chip);
                useChip.Add(EnmChip.SN76489);

                if (hiyorimiNecessary) hiyorimiNecessary = true;
                else hiyorimiNecessary = false;

                if (mds == null)
                    mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
                else
                    mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

                chipRegister.initChipRegister(lstChips.ToArray());

                SetYM2612Volume(true, setting.balance.YM2612Volume);
                SetSN76489Volume(true, setting.balance.SN76489Volume);
                //chipRegister.setYM2203SSGVolume(0, setting.balance.GimicOPNVolume, enmModel.RealModel);
                //chipRegister.setYM2203SSGVolume(1, setting.balance.GimicOPNVolume, enmModel.RealModel);
                //chipRegister.setYM2608SSGVolume(0, setting.balance.GimicOPNAVolume, enmModel.RealModel);
                //chipRegister.setYM2608SSGVolume(1, setting.balance.GimicOPNAVolume, enmModel.RealModel);

                if (!driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.YM2612, EnmChip.SN76489 }
                    , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
                    , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
                //if (driverReal != null)
                //{
                //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.YM2612, EnmChip.SN76489 }
                //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
                //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
                //}
                //Play

                Paused = false;
                //oneTimeReset = false;

                Thread.Sleep(500);

                Stopped = false;

                return true;
            }
            catch (Exception ex)
            {
                log.ForcedWrite(ex);
                return false;
            }

        }

        //public static bool s98Play(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        MDSound.MDSound.Chip chip;

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();

        //        MasterVolume = setting.balance.MasterVolume;

        //        if (!driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.YM2203 }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.YM2203 }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        List<S98.S98DevInfo> s98DInfo = ((S98)driver).s98Info.DeviceInfos;

        //        ay8910 ym2149 = null;
        //        ym2203 ym2203 = null;
        //        ym2612 ym2612 = null;
        //        ym3438 ym3438 = null;
        //        ym2608 ym2608 = null;
        //        ym2151 ym2151 = null;
        //        ym2151_mame ym2151mame = null;
        //        ym2151_x68sound ym2151_x68sound = null;
        //        ym2413 ym2413 = null;
        //        ay8910 ay8910 = null;

        //        int YM2151ClockValue = 4000000;
        //        int YM2203ClockValue = 4000000;
        //        int YM2413ClockValue = 4000000;
        //        int YM2608ClockValue = 8000000;
        //        useChip.Clear();

        //        foreach (S98.S98DevInfo dInfo in s98DInfo)
        //        {
        //            switch (dInfo.DeviceType)
        //            {
        //                case 1:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2149 == null)
        //                    {
        //                        ym2149 = new ay8910();
        //                        chip.ID = 0;
        //                        chipLED.PriAY10 = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecAY10 = 1;
        //                    }
        //                    chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
        //                    chip.Instrument = ym2149;
        //                    chip.Update = ym2149.Update;
        //                    chip.Start = ym2149.Start;
        //                    chip.Stop = ym2149.Stop;
        //                    chip.Reset = ym2149.Reset;
        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.AY8910Volume;
        //                    chip.Clock = dInfo.Clock / 4;
        //                    clockAY8910 = (int)chip.Clock;
        //                    chip.Option = null;
        //                    //hiyorimiDeviceFlag |= 0x2;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.AY8910 : EnmChip.S_AY8910);
        //                    break;
        //                case 2:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2203 == null)
        //                    {
        //                        ym2203 = new ym2203();
        //                        chip.ID = 0;
        //                        chipLED.PriOPN = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecOPN = 1;
        //                    }
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2203;
        //                    chip.Instrument = ym2203;
        //                    chip.Update = ym2203.Update;
        //                    chip.Start = ym2203.Start;
        //                    chip.Stop = ym2203.Stop;
        //                    chip.Reset = ym2203.Reset;
        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.YM2203Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    YM2203ClockValue = (int)chip.Clock;
        //                    chip.Option = null;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.YM2203 : EnmChip.S_YM2203);

        //                    break;
        //                case 3:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2612 == null)
        //                    {
        //                        ym2612 = new ym2612();
        //                        ym3438 = new ym3438();
        //                        chip.ID = 0;
        //                        chipLED.PriOPN = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecOPN = 1;
        //                    }

        //                    if ((chip.ID == 0 && setting.YM2612Type.UseEmu) || (chip.ID == 1 && setting.YM2612SType.UseEmu))
        //                    {
        //                        chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
        //                        chip.Instrument = ym2612;
        //                        chip.Update = ym2612.Update;
        //                        chip.Start = ym2612.Start;
        //                        chip.Stop = ym2612.Stop;
        //                        chip.Reset = ym2612.Reset;
        //                    }
        //                    else if ((chip.ID == 0 && setting.YM2612Type.UseEmu2) || (chip.ID == 1 && setting.YM2612SType.UseEmu2))
        //                    {
        //                        chip.type = MDSound.MDSound.enmInstrumentType.YM3438;
        //                        chip.Instrument = ym3438;
        //                        chip.Update = ym3438.Update;
        //                        chip.Start = ym3438.Start;
        //                        chip.Stop = ym3438.Stop;
        //                        chip.Reset = ym3438.Reset;
        //                        switch (setting.nukedOPN2.EmuType)
        //                        {
        //                            case 0:
        //                                ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.discrete);
        //                                break;
        //                            case 1:
        //                                ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic);
        //                                break;
        //                            case 2:
        //                                ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612);
        //                                break;
        //                            case 3:
        //                                ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612_u);
        //                                break;
        //                            case 4:
        //                                ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic_lp);
        //                                break;
        //                        }
        //                    }

        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.YM2612Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    chip.Option = null;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.YM2612 : EnmChip.S_YM2612);

        //                    break;
        //                case 4:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2608 == null)
        //                    {
        //                        ym2608 = new ym2608();
        //                        chip.ID = 0;
        //                        chipLED.PriOPNA = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecOPNA = 1;
        //                    }
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
        //                    chip.Instrument = ym2608;
        //                    chip.Update = ym2608.Update;
        //                    chip.Start = ym2608.Start;
        //                    chip.Stop = ym2608.Stop;
        //                    chip.Reset = ym2608.Reset;
        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.YM2608Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    YM2608ClockValue = (int)chip.Clock;
        //                    chip.Option = new object[] { Common.GetApplicationFolder() };
        //                    //hiyorimiDeviceFlag |= 0x2;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.YM2608 : EnmChip.S_YM2608);

        //                    break;
        //                case 5:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2151 == null && ym2151mame == null)
        //                    {
        //                        chip.ID = 0;
        //                        chipLED.PriOPM = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecOPM = 1;
        //                    }

        //                    if ((chip.ID == 0 && setting.YM2151Type.UseEmu) || (chip.ID == 1 && setting.YM2151SType.UseEmu))
        //                    {
        //                        if (ym2151 == null) ym2151 = new MDSound.ym2151();
        //                        chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
        //                        chip.Instrument = ym2151;
        //                        chip.Update = ym2151.Update;
        //                        chip.Start = ym2151.Start;
        //                        chip.Stop = ym2151.Stop;
        //                        chip.Reset = ym2151.Reset;
        //                    }
        //                    else if ((chip.ID == 0 && setting.YM2151Type.UseEmu2) || (chip.ID == 1 && setting.YM2151SType.UseEmu2))
        //                    {
        //                        if (ym2151mame == null) ym2151mame = new MDSound.ym2151_mame();
        //                        chip.type = MDSound.MDSound.enmInstrumentType.YM2151mame;
        //                        chip.Instrument = ym2151mame;
        //                        chip.Update = ym2151mame.Update;
        //                        chip.Start = ym2151mame.Start;
        //                        chip.Stop = ym2151mame.Stop;
        //                        chip.Reset = ym2151mame.Reset;
        //                    }
        //                    else if ((chip.ID == 0 && setting.YM2151Type.UseEmu3) || (chip.ID == 1 && setting.YM2151SType.UseEmu3))
        //                    {
        //                        if (ym2151_x68sound == null) ym2151_x68sound = new MDSound.ym2151_x68sound();
        //                        chip.type = MDSound.MDSound.enmInstrumentType.YM2151x68sound;
        //                        chip.Instrument = ym2151_x68sound;
        //                        chip.Update = ym2151_x68sound.Update;
        //                        chip.Start = ym2151_x68sound.Start;
        //                        chip.Stop = ym2151_x68sound.Stop;
        //                        chip.Reset = ym2151_x68sound.Reset;
        //                    }

        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.YM2151Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    YM2151ClockValue = (int)chip.Clock;
        //                    chip.Option = null;
        //                    //hiyorimiDeviceFlag |= 0x2;
        //                    if (chip.Start != null)
        //                        lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.YM2151 : EnmChip.S_YM2151);

        //                    break;
        //                case 6:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ym2413 == null)
        //                    {
        //                        ym2413 = new ym2413();
        //                        chip.ID = 0;
        //                        chipLED.PriOPLL = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecOPLL = 1;
        //                    }
        //                    chip.type = MDSound.MDSound.enmInstrumentType.YM2413;
        //                    chip.Instrument = ym2413;
        //                    chip.Update = ym2413.Update;
        //                    chip.Start = ym2413.Start;
        //                    chip.Stop = ym2413.Stop;
        //                    chip.Reset = ym2413.Reset;
        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.YM2413Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    YM2413ClockValue = (int)chip.Clock;
        //                    chip.Option = null;
        //                    //hiyorimiDeviceFlag |= 0x2;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.YM2413 : EnmChip.S_YM2413);

        //                    break;
        //                case 15:
        //                    chip = new MDSound.MDSound.Chip();
        //                    if (ay8910 == null)
        //                    {
        //                        ay8910 = new ay8910();
        //                        chip.ID = 0;
        //                        chipLED.PriAY10 = 1;
        //                    }
        //                    else
        //                    {
        //                        chip.ID = 1;
        //                        chipLED.SecAY10 = 1;
        //                    }
        //                    chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
        //                    chip.Instrument = ay8910;
        //                    chip.Update = ay8910.Update;
        //                    chip.Start = ay8910.Start;
        //                    chip.Stop = ay8910.Stop;
        //                    chip.Reset = ay8910.Reset;
        //                    chip.SamplingRate = (UInt32)Common.SampleRate;
        //                    chip.Volume = setting.balance.AY8910Volume;
        //                    chip.Clock = dInfo.Clock;
        //                    clockAY8910 = (int)chip.Clock;
        //                    chip.Option = null;
        //                    //hiyorimiDeviceFlag |= 0x2;
        //                    lstChips.Add(chip);
        //                    useChip.Add(chip.ID == 0 ? EnmChip.AY8910 : EnmChip.S_AY8910);

        //                    break;
        //            }
        //        }

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        if (useChip.Contains(EnmChip.YM2203) || useChip.Contains(EnmChip.S_YM2203))
        //        {
        //            SetYM2203Volume(true, setting.balance.YM2203Volume);
        //            SetYM2203FMVolume(true, setting.balance.YM2203FMVolume);
        //            SetYM2203PSGVolume(true, setting.balance.YM2203PSGVolume);
        //        }

        //        if (useChip.Contains(EnmChip.YM2612) || useChip.Contains(EnmChip.S_YM2612))
        //            SetYM2612Volume(true, setting.balance.YM2612Volume);

        //        if (useChip.Contains(EnmChip.YM2608) || useChip.Contains(EnmChip.S_YM2608))
        //        {
        //            SetYM2608Volume(true, setting.balance.YM2608Volume);
        //            SetYM2608FMVolume(true, setting.balance.YM2608FMVolume);
        //            SetYM2608PSGVolume(true, setting.balance.YM2608PSGVolume);
        //            SetYM2608RhythmVolume(true, setting.balance.YM2608RhythmVolume);
        //            SetYM2608AdpcmVolume(true, setting.balance.YM2608AdpcmVolume);
        //        }

        //        if (useChip.Contains(EnmChip.YM2608))
        //        {
        //            chipRegister.YM2608SetRegister(0,0, 0, 0x29, 0x82);
        //        }
        //        if (useChip.Contains(EnmChip.S_YM2608))
        //        {
        //            chipRegister.YM2608SetRegister(0, 1, 0, 0x29, 0x82);
        //        }
        //        if (useChip.Contains(EnmChip.YM2151) || useChip.Contains(EnmChip.S_YM2151))
        //            SetYM2151Volume(false, setting.balance.YM2151Volume);
        //        if (useChip.Contains(EnmChip.YM2413) || useChip.Contains(EnmChip.S_YM2413))
        //            SetYM2413Volume(true, setting.balance.YM2413Volume);
        //        if (useChip.Contains(EnmChip.AY8910) || useChip.Contains(EnmChip.S_AY8910))
        //            SetAY8910Volume(false, setting.balance.AY8910Volume);

        //        if (useChip.Contains(EnmChip.YM2151))
        //            chipRegister.YM2151WriteClock(0, YM2151ClockValue);
        //        if (useChip.Contains(EnmChip.S_YM2151))
        //            chipRegister.YM2151WriteClock(1, YM2151ClockValue);
        //        if (useChip.Contains(EnmChip.YM2203))
        //            chipRegister.YM2203WriteClock(0, YM2203ClockValue);
        //        if (useChip.Contains(EnmChip.S_YM2203))
        //            chipRegister.YM2203WriteClock(1, YM2203ClockValue);
        //        if (useChip.Contains(EnmChip.YM2413))
        //            chipRegister.YM2413WriteClock(0, YM2413ClockValue);
        //        if (useChip.Contains(EnmChip.S_YM2413))
        //            chipRegister.YM2413WriteClock(1, YM2413ClockValue);
        //        if (useChip.Contains(EnmChip.YM2608))
        //            chipRegister.YM2608WriteClock(0, YM2608ClockValue);
        //        if (useChip.Contains(EnmChip.S_YM2608))
        //            chipRegister.YM2608WriteClock(1, YM2608ClockValue);

        //        //driver.SetYM2151Hosei(YM2151ClockValue);

        //        //if (driverReal == null || ((S98)driverReal).SSGVolumeFromTAG == -1)
        //        if (((S98)driver).SSGVolumeFromTAG == -1)
        //        {
        //            if (useChip.Contains(EnmChip.YM2203))
        //                chipRegister.YM2203SetSSGVolume(0, setting.balance.GimicOPNVolume );
        //            if (useChip.Contains(EnmChip.S_YM2203))
        //                chipRegister.YM2203SetSSGVolume(1, setting.balance.GimicOPNVolume );
        //            if (useChip.Contains(EnmChip.YM2608))
        //                chipRegister.YM2608SetSSGVolume(0, setting.balance.GimicOPNAVolume);
        //            if (useChip.Contains(EnmChip.S_YM2608))
        //                chipRegister.YM2608SetSSGVolume(1, setting.balance.GimicOPNAVolume);
        //        }
        //        else
        //        {
        //            int SSGVolumeFromTAG = ((S98)driver).SSGVolumeFromTAG;

        //            if (useChip.Contains(EnmChip.YM2203))
        //                chipRegister.YM2203SetSSGVolume(0, SSGVolumeFromTAG);
        //            if (useChip.Contains(EnmChip.S_YM2203))
        //                chipRegister.YM2203SetSSGVolume(1, SSGVolumeFromTAG);
        //            if (useChip.Contains(EnmChip.YM2608))
        //                chipRegister.YM2608SetSSGVolume(0, SSGVolumeFromTAG);
        //            if (useChip.Contains(EnmChip.S_YM2608))
        //                chipRegister.YM2608SetSSGVolume(1, SSGVolumeFromTAG);
        //        }
        //        //Play

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool midPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        chipLED.PriMID = 1;
        //        chipLED.SecMID = 0;

        //        MasterVolume = setting.balance.MasterVolume;

        //        chipRegister.initChipRegister(null);
        //        chipRegister.MIDI.Model = EnmModel.RealModel;
        //        chipRegister.MIDI.Device = EnmDevice.MIDIGM;

        //        ReleaseAllMIDIout();
        //        MakeMIDIout(setting, MidiMode);
        //        chipRegister.setMIDIout(setting.midiOut.lstMidiOutInfo[MidiMode], midiOuts, midiOutsType, vstMidiOuts, vstMidiOutsType);

        //        for (int i = 0; i < setting.midiOut.lstMidiOutInfo[MidiMode].Length; i++)
        //        {
        //            midiOutInfo moi = setting.midiOut.lstMidiOutInfo[MidiMode][i];
        //            log.Write(string.Format(
        //                "{0}"
        //                , moi.name
        //                ));
        //        }

        //        if (!driver.init(vgmBuf, chipRegister, new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        useEmu = false;
        //        useReal = true;

        //        //Play

        //        PackData[] stopData = MakeSoftResetData();
        //        sm.SetStopData(stopData);

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool RcpPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        chipLED.PriMID = 1;
        //        chipLED.SecMID = 0;

        //        MasterVolume = setting.balance.MasterVolume;

        //        chipRegister.initChipRegister(null);
        //        chipRegister.MIDI.Model = EnmModel.RealModel;
        //        chipRegister.MIDI.Device = EnmDevice.MIDIGM;

        //        ReleaseAllMIDIout();
        //        MakeMIDIout(setting, MidiMode);
        //        chipRegister.setMIDIout(setting.midiOut.lstMidiOutInfo[MidiMode], midiOuts, midiOutsType, vstMidiOuts, vstMidiOutsType);

        //        for(int i = 0; i < setting.midiOut.lstMidiOutInfo[MidiMode].Length; i++)
        //        {
        //            midiOutInfo moi = setting.midiOut.lstMidiOutInfo[MidiMode][i];
        //            log.Write(string.Format(
        //                "{0}"
        //                ,moi.name
        //                ));
        //        }

        //        if (!driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        useEmu = false;
        //        useReal = true;

        //        //Play

        //        PackData[] stopData = MakeSoftResetData();
        //        sm.SetStopData(stopData);

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool nsfPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();


        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        chipLED.PriNES = 1;
        //        chipLED.PriDMC = 1;

        //        MasterVolume = setting.balance.MasterVolume;

        //        ((nsf)driver).song = SongNo;
        //        if (!driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    ((nsf)driverReal).song = SongNo;
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        if (((nsf)driver).use_fds) chipLED.PriFDS = 1;
        //        if (((nsf)driver).use_fme7) chipLED.PriFME7 = 1;
        //        if (((nsf)driver).use_mmc5) chipLED.PriMMC5 = 1;
        //        if (((nsf)driver).use_n106) chipLED.PriN160 = 1;
        //        if (((nsf)driver).use_vrc6) chipLED.PriVRC6 = 1;
        //        if (((nsf)driver).use_vrc7) chipLED.PriVRC7 = 1;

        //        //nes_intf nes = new nes_intf();
        //        MDSound.MDSound.Chip chip;
        //        nes_intf nes = new nes_intf();

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.Nes;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Volume = setting.balance.APUVolume;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cAPU = chip;
        //        useChip.Add(EnmChip.NES);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.DMC;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.DMCVolume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cDMC = chip;
        //        useChip.Add(EnmChip.DMC);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.FDS;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.FDSVolume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cFDS = chip;
        //        useChip.Add(EnmChip.FDS);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.MMC5;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.MMC5Volume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cMMC5 = chip;
        //        useChip.Add(EnmChip.MMC5);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.N160;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.N160Volume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cN160 = chip;
        //        useChip.Add(EnmChip.N160);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.VRC6;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.VRC6Volume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cVRC6 = chip;
        //        useChip.Add(EnmChip.VRC6);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.VRC7;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.VRC7Volume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cVRC7 = chip;
        //        useChip.Add(EnmChip.VRC7);

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.FME7;
        //        chip.Instrument = nes;
        //        chip.Update = nes.Update;
        //        chip.Start = nes.Start;
        //        chip.Stop = nes.Stop;
        //        chip.Reset = nes.Reset;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Clock = 0;
        //        chip.Option = null;
        //        chip.Volume = setting.balance.FME7Volume;
        //        lstChips.Add(chip);
        //        ((nsf)driver).cFME7 = chip;
        //        useChip.Add(EnmChip.FME7);

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegisterNSF(lstChips.ToArray());

        //        //Play

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool hesPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        //Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        chipLED.PriHuC = 1;

        //        MasterVolume = setting.balance.MasterVolume;

        //        //((hes)driverVirtual).song = (byte)SongNo;
        //        //((hes)driverReal).song = (byte)SongNo;
        //        //if (!driverVirtual.init(vgmBuf, chipRegister, enmModel.VirtualModel, new enmUseChip[] { enmUseChip.Unuse }, 0)) return false;
        //        //if (!driverReal.init(vgmBuf, chipRegister, enmModel.RealModel, new enmUseChip[] { enmUseChip.Unuse }, 0)) return false;

        //        MDSound.MDSound.Chip chip;
        //        MDSound.Ootake_PSG huc = new Ootake_PSG();

        //        chip = new MDSound.MDSound.Chip();
        //        chip.ID = 0;
        //        chip.type = MDSound.MDSound.enmInstrumentType.HuC6280;
        //        chip.Instrument = huc;
        //        chip.Update = huc.Update;
        //        chip.Start = huc.Start;
        //        chip.Stop = huc.Stop;
        //        chip.Reset = huc.Reset;
        //        chip.AdditionalUpdate = ((hes)driver).AdditionalUpdate;
        //        chip.SamplingRate = (UInt32)Common.SampleRate;
        //        chip.Volume = setting.balance.HuC6280Volume;
        //        chip.Clock = 3579545;
        //        chip.Option = null;
        //        lstChips.Add(chip);
        //        ((hes)driver).c6280 = chip;
        //        useChip.Add(EnmChip.HuC6280);

        //        if (hiyorimiNecessary) hiyorimiNecessary = true;
        //        else hiyorimiNecessary = false;

        //        if (mds == null)
        //            mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
        //        else
        //            mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

        //        chipRegister.initChipRegister(lstChips.ToArray());

        //        ((hes)driver).song = (byte)SongNo;
        //        if (!driver.init(vgmBuf, chipRegister, new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    ((hes)driverReal).song = (byte)SongNo;
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}
        //        //Play

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        //public static bool sidPlay(Setting setting)
        //{

        //    try
        //    {

        //        if (vgmBuf == null || setting == null) return false;

        //        Stop();

        //        //chipRegister.resetChips();
        //        ResetFadeOutParam();
        //        chipRegister.initChipRegister(null);

        //        useChip.Clear();

        //        //startTrdVgmReal();

        //        List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

        //        hiyorimiNecessary = setting.HiyorimiMode;

        //        chipLED = new ChipLEDs();
        //        chipLED.PriSID = 1;

        //        MasterVolume = setting.balance.MasterVolume;

        //        ((Driver.SID.sid)driver).song = (byte)SongNo + 1;
        //        if (!driver.init(vgmBuf, chipRegister,  new EnmChip[] { EnmChip.Unuse }
        //            , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
        //            , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //if (driverReal != null)
        //        //{
        //        //    ((Driver.SID.sid)driverReal).song = (byte)SongNo + 1;
        //        //    if (!driverReal.init(vgmBuf, chipRegister, EnmModel.RealModel, new EnmChip[] { EnmChip.Unuse }
        //        //        , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
        //        //        , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000))) return false;
        //        //}

        //        Paused = false;
        //        //oneTimeReset = false;

        //        Thread.Sleep(500);

        //        Stopped = false;

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ForcedWrite(ex);
        //        return false;
        //    }

        //}

        public static bool vgmPlay(Setting setting)
        {

            try
            {

                if (vgmBuf == null || setting == null) return false;

                vgm vgmDriver = (vgm)driver;

                //chipRegister.resetChips();
                ResetFadeOutParam();
                useChip.Clear();

                //startTrdVgmReal();

                List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();

                MDSound.MDSound.Chip chip;

                hiyorimiNecessary = setting.HiyorimiMode;

                chipLED = new ChipLEDs();

                MasterVolume = setting.balance.MasterVolume;

                log.Write("ドライバ(VGM)初期化");

                if (!driver.init(vgmBuf
                    , chipRegister
                    
                    , new EnmChip[] { EnmChip.YM2203 }// usechip.ToArray()
                    , (uint)(Common.SampleRate * setting.LatencyEmulation / 1000)
                    , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)))
                    return false;

                //if (driverReal != null && !driverReal.init(vgmBuf
                //    , chipRegister
                //    , EnmModel.RealModel
                //    , new EnmChip[] { EnmChip.YM2203 }// usechip.ToArray()
                //    , (uint)(Common.SampleRate * setting.LatencySCCI / 1000)
                //    , (uint)(Common.SampleRate * setting.outputDevice.WaitTime / 1000)))
                //    return false;

                hiyorimiNecessary = setting.HiyorimiMode;
                int hiyorimiDeviceFlag = 0;

                chipLED = new ChipLEDs();

                MasterVolume = setting.balance.MasterVolume;


                {
                    log.Write("使用チップの調査");

                    chipRegister.ClearChipParam();

                    if (vgmDriver.AY8910ClockValue != 0)
                    {
                        MDSound.ay8910 ay8910 = new MDSound.ay8910();
                        for (int i = 0; i < (((vgm)driver).AY8910DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.AY8910;
                            chip.ID = (byte)i;
                            chip.Instrument = ay8910;
                            chip.Update = ay8910.Update;
                            chip.Start = ay8910.Start;
                            chip.Stop = ay8910.Stop;
                            chip.Reset = ay8910.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.AY8910Volume;
                            chip.Clock = (((vgm)driver).AY8910ClockValue & 0x7fffffff) / 2;
                            clockAY8910 = (int)chip.Clock;
                            chip.Option = null;
                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriAY10 = 1;
                                useChip.Add(EnmChip.AY8910);
                            }
                            else
                            {
                                chipLED.SecAY10 = 1;
                                useChip.Add(EnmChip.S_AY8910);
                            }

                            log.Write(string.Format("Use AY8910({0}) Clk:{1}"
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.AY8910[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.C140ClockValue != 0)
                    {
                        MDSound.c140 c140 = new MDSound.c140();
                        for (int i = 0; i < (((vgm)driver).C140DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.C140;
                            chip.ID = (byte)i;
                            chip.Instrument = c140;
                            chip.Update = c140.Update;
                            chip.Start = c140.Start;
                            chip.Stop = c140.Stop;
                            chip.Reset = c140.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.C140Volume;
                            chip.Clock = ((vgm)driver).C140ClockValue;
                            chip.Option = new object[1] { ((vgm)driver).C140Type };

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriC140 = 1;
                                useChip.Add(EnmChip.C140);
                            }
                            else
                            {
                                chipLED.SecC140 = 1;
                                useChip.Add(EnmChip.S_C140);
                            }

                            log.Write(string.Format("Use C140({0}) Clk:{1} Type:{2}"
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                , chip.Option[0]
                                ));

                            chipRegister.C140[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.SEGAPCMClockValue != 0)
                    {
                        chip = new MDSound.MDSound.Chip();
                        chip.type = MDSound.MDSound.enmInstrumentType.SEGAPCM;
                        chip.ID = 0;
                        MDSound.segapcm segapcm = new MDSound.segapcm();
                        chip.Instrument = segapcm;
                        chip.Update = segapcm.Update;
                        chip.Start = segapcm.Start;
                        chip.Stop = segapcm.Stop;
                        chip.Reset = segapcm.Reset;
                        chip.SamplingRate = (UInt32)Common.SampleRate;
                        chip.Volume = setting.balance.SEGAPCMVolume;
                        chip.Clock = ((vgm)driver).SEGAPCMClockValue;
                        chip.Option = new object[1] { ((vgm)driver).SEGAPCMInterface };

                        hiyorimiDeviceFlag |= 0x2;

                        chipLED.PriSPCM = 1;
                        useChip.Add(EnmChip.SEGAPCM);

                        log.Write(string.Format("Use SEGAPCM({0}) Clk:{1} Model:{2} Type:{3}"
                            , (0 == 0) ? "Pri" : "Sec"
                            , chip.Clock
                            , chipRegister.SEGAPCM[0].Model
                            , chip.Option[0]
                            ));

                        chipRegister.SEGAPCM[0].Use = true;

                        lstChips.Add(chip);
                    }

                    if (vgmDriver.SN76489ClockValue != 0)
                    {
                        MDSound.sn76489 sn76489 = new MDSound.sn76489();

                        for (int i = 0; i < (((vgm)driver).SN76489DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.SN76489;
                            chip.ID = (byte)i;
                            chip.Instrument = sn76489;
                            chip.Update = sn76489.Update;
                            chip.Start = sn76489.Start;
                            chip.Stop = sn76489.Stop;
                            chip.Reset = sn76489.Reset;

                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.SN76489Volume;
                            chip.Clock = ((vgm)driver).SN76489ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= (setting.SN76489Type.UseScci) ? 0x1 : 0x2;

                            if (i == 0)
                            {
                                chipLED.PriDCSG = 1;
                                useChip.Add(EnmChip.SN76489);
                            }
                            else
                            {
                                chipLED.SecDCSG = 1;
                                useChip.Add(EnmChip.S_SN76489);
                            }

                            log.Write(string.Format("Use DCSG({0}) Clk:{1}"
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.SN76489[i].Use = true;
                            lstChips.Add(chip);

                        }
                    }

                    if (vgmDriver.YM2151ClockValue != 0)
                    {
                        MDSound.ym2151 ym2151 = null;
                        MDSound.ym2151_mame ym2151_mame = null;
                        MDSound.ym2151_x68sound ym2151_x68sound = null;

                        for (int i = 0; i < (((vgm)driver).YM2151DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.ID = (byte)i;

                            if ((i == 0 && setting.YM2151Type.UseEmu) || (i == 1 && setting.YM2151SType.UseEmu))
                            {
                                if (ym2151 == null) ym2151 = new MDSound.ym2151();
                                chip.type = MDSound.MDSound.enmInstrumentType.YM2151;
                                chip.Instrument = ym2151;
                                chip.Update = ym2151.Update;
                                chip.Start = ym2151.Start;
                                chip.Stop = ym2151.Stop;
                                chip.Reset = ym2151.Reset;
                            }
                            else if ((i == 0 && setting.YM2151Type.UseEmu2) || (i == 1 && setting.YM2151SType.UseEmu2))
                            {
                                if (ym2151_mame == null) ym2151_mame = new MDSound.ym2151_mame();
                                chip.type = MDSound.MDSound.enmInstrumentType.YM2151mame;
                                chip.Instrument = ym2151_mame;
                                chip.Update = ym2151_mame.Update;
                                chip.Start = ym2151_mame.Start;
                                chip.Stop = ym2151_mame.Stop;
                                chip.Reset = ym2151_mame.Reset;
                            }
                            else if ((i == 0 && setting.YM2151Type.UseEmu3) || (i == 1 && setting.YM2151SType.UseEmu3))
                            {
                                if (ym2151_x68sound == null) ym2151_x68sound = new MDSound.ym2151_x68sound();
                                chip.type = MDSound.MDSound.enmInstrumentType.YM2151x68sound;
                                chip.Instrument = ym2151_x68sound;
                                chip.Update = ym2151_x68sound.Update;
                                chip.Start = ym2151_x68sound.Start;
                                chip.Stop = ym2151_x68sound.Stop;
                                chip.Reset = ym2151_x68sound.Reset;
                            }

                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2151Volume;
                            chip.Clock = ((vgm)driver).YM2151ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriOPM = 1;
                                useChip.Add(EnmChip.YM2151);
                            }
                            else
                            {
                                chipLED.SecOPM = 1;
                                useChip.Add(EnmChip.YM2151);
                            }

                            log.Write(string.Format("Use OPM({0}) Clk:{1} "
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.YM2151[i].Use = true;
                            if (chip.Start != null) lstChips.Add(chip);

                        }
                    }

                    if (vgmDriver.YM2203ClockValue != 0)
                    {
                        MDSound.ym2203 ym2203 = new MDSound.ym2203();
                        for (int i = 0; i < (((vgm)driver).YM2203DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM2203;
                            chip.ID = (byte)i;
                            chip.Instrument = ym2203;
                            chip.Update = ym2203.Update;
                            chip.Start = ym2203.Start;
                            chip.Stop = ym2203.Stop;
                            chip.Reset = ym2203.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2203Volume;
                            chip.Clock = ((vgm)driver).YM2203ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriOPN = 1;
                                useChip.Add(EnmChip.YM2203);
                            }
                            else
                            {
                                chipLED.SecOPN = 1;
                                useChip.Add(EnmChip.YM2203);
                            }

                            log.Write(string.Format("Use OPN({0}) Clk:{1} "
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.YM2203[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.YM2413ClockValue != 0)
                    {
                        MDSound.ym2413 ym2413 = new MDSound.ym2413();

                        for (int i = 0; i < (((vgm)driver).YM2413DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM2413;
                            chip.ID = (byte)i;
                            chip.Instrument = ym2413;
                            chip.Update = ym2413.Update;
                            chip.Start = ym2413.Start;
                            chip.Stop = ym2413.Stop;
                            chip.Reset = ym2413.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2413Volume;
                            chip.Clock = (((vgm)driver).YM2413ClockValue & 0x7fffffff);
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriOPLL = 1;
                                useChip.Add(EnmChip.YM2413);
                            }
                            else
                            {
                                chipLED.SecOPLL = 1;
                                useChip.Add(EnmChip.YM2413);
                            }

                            log.Write(string.Format("Use OPLL({0}) Clk:{1} "
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.YM2413[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.YM2608ClockValue != 0)
                    {
                        MDSound.ym2608 ym2608 = new MDSound.ym2608();
                        for (int i = 0; i < (vgmDriver.YM2608DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM2608;
                            chip.ID = (byte)i;
                            chip.Instrument = ym2608;
                            chip.Update = ym2608.Update;
                            chip.Start = ym2608.Start;
                            chip.Stop = ym2608.Stop;
                            chip.Reset = ym2608.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2608Volume;
                            chip.Clock = vgmDriver.YM2608ClockValue;
                            chip.Option = new object[] { Common.GetApplicationFolder() };
                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriOPNA = 1;
                                useChip.Add(EnmChip.YM2608);
                            }
                            else
                            {
                                chipLED.SecOPNA = 1;
                                useChip.Add(EnmChip.S_YM2608);
                            }

                            log.Write(string.Format("Use OPNA({0}) Clk:{1}"
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.YM2608[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.YM2610ClockValue != 0)
                    {
                        MDSound.ym2610 ym2610 = new MDSound.ym2610();
                        for (int i = 0; i < (((vgm)driver).YM2610DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM2610;
                            chip.ID = (byte)i;
                            chip.Instrument = ym2610;
                            chip.Update = ym2610.Update;
                            chip.Start = ym2610.Start;
                            chip.Stop = ym2610.Stop;
                            chip.Reset = ym2610.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2610Volume;
                            chip.Clock = ((vgm)driver).YM2610ClockValue & 0x7fffffff;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0)
                            {
                                chipLED.PriOPNB = 1;
                                useChip.Add(EnmChip.YM2610);
                            }
                            else
                            {
                                chipLED.SecOPNB = 1;
                                useChip.Add(EnmChip.S_YM2610);
                            }

                            log.Write(string.Format("Use OPNB({0}) Clk:{1} "
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                ));

                            chipRegister.YM2610[i].Use = true;
                            lstChips.Add(chip);
                        }
                    }

                    if (vgmDriver.YM2612ClockValue != 0)
                    {
                        MDSound.ym2612 ym2612 = null;
                        MDSound.ym3438 ym3438 = null;

                        for (int i = 0; i < (((vgm)driver).YM2612DualChipFlag ? 2 : 1); i++)
                        {
                            //MDSound.ym2612 ym2612 = new MDSound.ym2612();
                            chip = new MDSound.MDSound.Chip();
                            chip.ID = (byte)i;

                            if ((i == 0 && (setting.YM2612Type.UseEmu || setting.YM2612Type.UseScci))
                                || (i == 1 && setting.YM2612SType.UseEmu || setting.YM2612SType.UseScci))
                            {
                                if (ym2612 == null) ym2612 = new ym2612();
                                chip.type = MDSound.MDSound.enmInstrumentType.YM2612;
                                chip.Instrument = ym2612;
                                chip.Update = ym2612.Update;
                                chip.Start = ym2612.Start;
                                chip.Stop = ym2612.Stop;
                                chip.Reset = ym2612.Reset;
                            }
                            else if ((i == 0 && setting.YM2612Type.UseEmu2) || (i == 1 && setting.YM2612SType.UseEmu2))
                            {
                                if (ym3438 == null) ym3438 = new ym3438();
                                chip.type = MDSound.MDSound.enmInstrumentType.YM3438;
                                chip.Instrument = ym3438;
                                chip.Update = ym3438.Update;
                                chip.Start = ym3438.Start;
                                chip.Stop = ym3438.Stop;
                                chip.Reset = ym3438.Reset;
                                switch (setting.nukedOPN2.EmuType)
                                {
                                    case 0:
                                        ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.discrete);
                                        break;
                                    case 1:
                                        ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic);
                                        break;
                                    case 2:
                                        ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612);
                                        break;
                                    case 3:
                                        ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.ym2612_u);
                                        break;
                                    case 4:
                                        ym3438.OPN2_SetChipType(ym3438_const.ym3438_type.asic_lp);
                                        break;
                                }
                            }

                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM2612Volume;
                            chip.Clock = ((vgm)driver).YM2612ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= (setting.YM2612Type.UseScci) ? 0x1 : 0x2;
                            hiyorimiDeviceFlag |= (setting.YM2612Type.UseScci && setting.YM2612Type.OnlyPCMEmulation) ? 0x2 : 0x0;

                            if (i == 0)
                            {
                                chipLED.PriOPN2 = 1;
                                useChip.Add(EnmChip.YM2612);
                            }
                            else
                            {
                                chipLED.SecOPN2 = 1;
                                useChip.Add(EnmChip.S_YM2612);
                            }

                            log.Write(string.Format("Use OPN2({0}) Clk:{1} NukedOPN2Type:{2}"
                                , (i == 0) ? "Pri" : "Sec"
                                , chip.Clock
                                , setting.nukedOPN2.EmuType));

                            chipRegister.YM2612[i].Use = true;
                            lstChips.Add(chip);

                        }
                    }

                    if (vgmDriver.RF5C68ClockValue != 0)
                    {
                        MDSound.rf5c68 rf5c68 = new MDSound.rf5c68();

                        for (int i = 0; i < (((vgm)driver).RF5C68DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.RF5C68;
                            chip.ID = (byte)i;
                            chip.Instrument = rf5c68;
                            chip.Update = rf5c68.Update;
                            chip.Start = rf5c68.Start;
                            chip.Stop = rf5c68.Stop;
                            chip.Reset = rf5c68.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.RF5C68Volume;
                            chip.Clock = ((vgm)driver).RF5C68ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.RF5C68 : EnmChip.S_RF5C68);
                        }
                    }

                    if (vgmDriver.RF5C164ClockValue != 0)
                    {
                        MDSound.scd_pcm rf5c164 = new MDSound.scd_pcm();

                        for (int i = 0; i < (((vgm)driver).RF5C164DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.RF5C164;
                            chip.ID = (byte)i;
                            chip.Instrument = rf5c164;
                            chip.Update = rf5c164.Update;
                            chip.Start = rf5c164.Start;
                            chip.Stop = rf5c164.Stop;
                            chip.Reset = rf5c164.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.RF5C164Volume;
                            chip.Clock = ((vgm)driver).RF5C164ClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriRF5C = 1;
                            else chipLED.SecRF5C = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.RF5C164 : EnmChip.S_RF5C164);
                        }
                    }

                    if (vgmDriver.PWMClockValue != 0)
                    {
                        chip = new MDSound.MDSound.Chip();
                        chip.type = MDSound.MDSound.enmInstrumentType.PWM;
                        chip.ID = 0;
                        MDSound.pwm pwm = new MDSound.pwm();
                        chip.Instrument = pwm;
                        chip.Update = pwm.Update;
                        chip.Start = pwm.Start;
                        chip.Stop = pwm.Stop;
                        chip.Reset = pwm.Reset;
                        chip.SamplingRate = (UInt32)Common.SampleRate;
                        chip.Volume = setting.balance.PWMVolume;
                        chip.Clock = ((vgm)driver).PWMClockValue;
                        chip.Option = null;

                        hiyorimiDeviceFlag |= 0x2;

                        chipLED.PriPWM = 1;

                        lstChips.Add(chip);
                        useChip.Add(EnmChip.PWM);
                    }

                    if (vgmDriver.MultiPCMClockValue != 0)
                    {
                        MDSound.multipcm multipcm = new MDSound.multipcm();
                        for (int i = 0; i < (((vgm)driver).MultiPCMDualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.MultiPCM;
                            chip.ID = (byte)i;
                            chip.Instrument = multipcm;
                            chip.Update = multipcm.Update;
                            chip.Start = multipcm.Start;
                            chip.Stop = multipcm.Stop;
                            chip.Reset = multipcm.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.MultiPCMVolume;
                            chip.Clock = ((vgm)driver).MultiPCMClockValue;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriMPCM = 1;
                            else chipLED.SecMPCM = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.MultiPCM : EnmChip.S_MultiPCM);
                        }
                    }

                    if (vgmDriver.OKIM6258ClockValue != 0)
                    {
                        chip = new MDSound.MDSound.Chip();
                        chip.type = MDSound.MDSound.enmInstrumentType.OKIM6258;
                        chip.ID = 0;
                        MDSound.okim6258 okim6258 = new MDSound.okim6258();
                        chip.Instrument = okim6258;
                        chip.Update = okim6258.Update;
                        chip.Start = okim6258.Start;
                        chip.Stop = okim6258.Stop;
                        chip.Reset = okim6258.Reset;
                        chip.SamplingRate = (UInt32)Common.SampleRate;
                        chip.Volume = setting.balance.OKIM6258Volume;
                        chip.Clock = ((vgm)driver).OKIM6258ClockValue;
                        chip.Option = new object[1] { (int)((vgm)driver).OKIM6258Type };
                        //chip.Option = new object[1] { 6 };
                        okim6258.okim6258_set_srchg_cb(0, ChangeChipSampleRate, chip);

                        hiyorimiDeviceFlag |= 0x2;

                        chipLED.PriOKI5 = 1;

                        lstChips.Add(chip);
                        useChip.Add(EnmChip.OKIM6258);
                    }

                    if (vgmDriver.OKIM6295ClockValue != 0)
                    {
                        MDSound.okim6295 okim6295 = new MDSound.okim6295();
                        for (byte i = 0; i < (((vgm)driver).OKIM6295DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.OKIM6295;
                            chip.ID = (byte)i;
                            chip.Instrument = okim6295;
                            chip.Update = okim6295.Update;
                            chip.Start = okim6295.Start;
                            chip.Stop = okim6295.Stop;
                            chip.Reset = okim6295.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.OKIM6295Volume;
                            chip.Clock = ((vgm)driver).OKIM6295ClockValue;
                            chip.Option = null;
                            okim6295.okim6295_set_srchg_cb(i, ChangeChipSampleRate, chip);

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriOKI9 = 1;
                            else chipLED.SecOKI9 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.OKIM6295 : EnmChip.S_OKIM6295);
                        }
                    }

                    if (vgmDriver.YM3812ClockValue != 0)
                    {
                        MDSound.ym3812 ym3812 = new MDSound.ym3812();
                        for (int i = 0; i < (((vgm)driver).YM3812DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM3812;
                            chip.ID = (byte)i;
                            chip.Instrument = ym3812;
                            chip.Update = ym3812.Update;
                            chip.Start = ym3812.Start;
                            chip.Stop = ym3812.Stop;
                            chip.Reset = ym3812.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM3812Volume;
                            chip.Clock = ((vgm)driver).YM3812ClockValue & 0x7fffffff;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriOPL2 = 1;
                            else chipLED.SecOPL2 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YM3812 : EnmChip.S_YM3812);
                        }
                    }

                    if (vgmDriver.YMF262ClockValue != 0)
                    {
                        MDSound.ymf262 ymf262 = new MDSound.ymf262();
                        for (int i = 0; i < (((vgm)driver).YMF262DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YMF262;
                            chip.ID = (byte)i;
                            chip.Instrument = ymf262;
                            chip.Update = ymf262.Update;
                            chip.Start = ymf262.Start;
                            chip.Stop = ymf262.Stop;
                            chip.Reset = ymf262.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YMF262Volume;
                            chip.Clock = ((vgm)driver).YMF262ClockValue & 0x7fffffff;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriOPL3 = 1;
                            else chipLED.SecOPL3 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YMF262 : EnmChip.S_YMF262);
                        }
                    }

                    if (vgmDriver.YMF271ClockValue != 0)
                    {
                        MDSound.ymf271 ymf271 = new MDSound.ymf271();
                        for (int i = 0; i < (((vgm)driver).YMF271DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YMF271;
                            chip.ID = (byte)i;
                            chip.Instrument = ymf271;
                            chip.Update = ymf271.Update;
                            chip.Start = ymf271.Start;
                            chip.Stop = ymf271.Stop;
                            chip.Reset = ymf271.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YMF271Volume;
                            chip.Clock = ((vgm)driver).YMF271ClockValue & 0x7fffffff;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriOPX = 1;
                            else chipLED.SecOPX = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YMF271 : EnmChip.S_YMF271);
                        }
                    }

                    if (vgmDriver.YMF278BClockValue != 0)
                    {
                        MDSound.ymf278b ymf278b = new MDSound.ymf278b();
                        for (int i = 0; i < (((vgm)driver).YMF278BDualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YMF278B;
                            chip.ID = (byte)i;
                            chip.Instrument = ymf278b;
                            chip.Update = ymf278b.Update;
                            chip.Start = ymf278b.Start;
                            chip.Stop = ymf278b.Stop;
                            chip.Reset = ymf278b.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YMF278BVolume;
                            chip.Clock = ((vgm)driver).YMF278BClockValue & 0x7fffffff;
                            chip.Option = new object[] { Common.GetApplicationFolder() };

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriOPL4 = 1;
                            else chipLED.SecOPL4 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YMF278B : EnmChip.S_YMF278B);
                        }
                    }

                    if (vgmDriver.YMZ280BClockValue != 0)
                    {
                        MDSound.ymz280b ymz280b = new MDSound.ymz280b();
                        for (int i = 0; i < (((vgm)driver).YMZ280BDualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YMZ280B;
                            chip.ID = (byte)i;
                            chip.Instrument = ymz280b;
                            chip.Update = ymz280b.Update;
                            chip.Start = ymz280b.Start;
                            chip.Stop = ymz280b.Stop;
                            chip.Reset = ymz280b.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YMZ280BVolume;
                            chip.Clock = ((vgm)driver).YMZ280BClockValue & 0x7fffffff;
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriYMZ = 1;
                            else chipLED.SecYMZ = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YMZ280B : EnmChip.S_YMZ280B);
                        }
                    }

                    if (vgmDriver.HuC6280ClockValue != 0)
                    {
                        MDSound.Ootake_PSG huc6280 = new MDSound.Ootake_PSG();
                        for (int i = 0; i < (((vgm)driver).HuC6280DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.HuC6280;
                            chip.ID = (byte)i;
                            chip.Instrument = huc6280;
                            chip.Update = huc6280.Update;
                            chip.Start = huc6280.Start;
                            chip.Stop = huc6280.Stop;
                            chip.Reset = huc6280.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.HuC6280Volume;
                            chip.Clock = (((vgm)driver).HuC6280ClockValue & 0x7fffffff);
                            chip.Option = null;

                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriHuC = 1;
                            else chipLED.SecHuC = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.HuC6280 : EnmChip.S_HuC6280);
                        }
                    }

                    if (vgmDriver.QSoundClockValue != 0)
                    {
                        MDSound.qsound qsound = new MDSound.qsound();
                        chip = new MDSound.MDSound.Chip();
                        chip.type = MDSound.MDSound.enmInstrumentType.QSound;
                        chip.ID = (byte)0;
                        chip.Instrument = qsound;
                        chip.Update = qsound.Update;
                        chip.Start = qsound.Start;
                        chip.Stop = qsound.Stop;
                        chip.Reset = qsound.Reset;
                        chip.SamplingRate = (UInt32)Common.SampleRate;
                        chip.Volume = setting.balance.QSoundVolume;
                        chip.Clock = (((vgm)driver).QSoundClockValue);// & 0x7fffffff);
                        chip.Option = null;

                        hiyorimiDeviceFlag |= 0x2;

                        //if (i == 0) chipLED.PriHuC = 1;
                        //else chipLED.SecHuC = 1;
                        chipLED.PriQsnd = 1;

                        lstChips.Add(chip);
                        useChip.Add(EnmChip.QSound);
                    }

                    if (vgmDriver.C352ClockValue != 0)
                    {
                        MDSound.c352 c352 = new c352();
                        for (int i = 0; i < (((vgm)driver).C352DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.C352;
                            chip.ID = (byte)i;
                            chip.Instrument = c352;
                            chip.Update = c352.Update;
                            chip.Start = c352.Start;
                            chip.Stop = c352.Stop;
                            chip.Reset = c352.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.C352Volume;
                            chip.Clock = (((vgm)driver).C352ClockValue & 0x7fffffff);
                            chip.Option = new object[1] { (((vgm)driver).C352ClockDivider) };
                            int divider = (ushort)((((vgm)driver).C352ClockDivider) != 0 ? (((vgm)driver).C352ClockDivider) : 288);
                            clockC352 = (int)(chip.Clock / divider);
                            c352.c352_set_options((byte)(((vgm)driver).C352ClockValue >> 31));
                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriC352 = 1;
                            else chipLED.SecC352 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.C352 : EnmChip.S_C352);
                        }
                    }

                    if (vgmDriver.GA20ClockValue != 0)
                    {
                        MDSound.iremga20 ga20 = new iremga20();
                        for (int i = 0; i < (((vgm)driver).GA20DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.GA20;
                            chip.ID = (byte)i;
                            chip.Instrument = ga20;
                            chip.Update = ga20.Update;
                            chip.Start = ga20.Start;
                            chip.Stop = ga20.Stop;
                            chip.Reset = ga20.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.GA20Volume;
                            chip.Clock = (((vgm)driver).GA20ClockValue & 0x7fffffff);
                            chip.Option = null;
                            hiyorimiDeviceFlag |= 0x2;

                            if (i == 0) chipLED.PriGA20 = 1;
                            else chipLED.SecGA20 = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.GA20 : EnmChip.S_GA20);
                        }
                    }

                    if (vgmDriver.K053260ClockValue != 0)
                    {
                        MDSound.K053260 k053260 = new MDSound.K053260();

                        for (int i = 0; i < (((vgm)driver).K053260DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.K053260;
                            chip.ID = (byte)i;
                            chip.Instrument = k053260;
                            chip.Update = k053260.Update;
                            chip.Start = k053260.Start;
                            chip.Stop = k053260.Stop;
                            chip.Reset = k053260.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.K053260Volume;
                            chip.Clock = ((vgm)driver).K053260ClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriK053260 = 1;
                            else chipLED.SecK053260 = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.K053260 : EnmChip.S_K053260);
                        }
                    }

                    if (vgmDriver.K054539ClockValue != 0)
                    {
                        MDSound.K054539 k054539 = new MDSound.K054539();

                        for (int i = 0; i < (((vgm)driver).K054539DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.K054539;
                            chip.ID = (byte)i;
                            chip.Instrument = k054539;
                            chip.Update = k054539.Update;
                            chip.Start = k054539.Start;
                            chip.Stop = k054539.Stop;
                            chip.Reset = k054539.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.K054539Volume;
                            chip.Clock = ((vgm)driver).K054539ClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriK054539 = 1;
                            else chipLED.SecK054539 = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.K054539 : EnmChip.S_K054539);
                        }
                    }

                    if (vgmDriver.K051649ClockValue != 0)
                    {
                        MDSound.K051649 k051649 = new MDSound.K051649();

                        for (int i = 0; i < (((vgm)driver).K051649DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.K051649;
                            chip.ID = (byte)i;
                            chip.Instrument = k051649;
                            chip.Update = k051649.Update;
                            chip.Start = k051649.Start;
                            chip.Stop = k051649.Stop;
                            chip.Reset = k051649.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.K051649Volume;
                            chip.Clock = ((vgm)driver).K051649ClockValue;
                            clockK051649 = (int)chip.Clock;
                            chip.Option = null;
                            if (i == 0) chipLED.PriK051649 = 1;
                            else chipLED.SecK051649 = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.K051649 : EnmChip.S_K051649);
                        }
                    }

                    if (vgmDriver.YM3526ClockValue != 0)
                    {
                        MDSound.ym3526 ym3526 = new MDSound.ym3526();

                        for (int i = 0; i < (((vgm)driver).YM3526DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.YM3526;
                            chip.ID = (byte)i;
                            chip.Instrument = ym3526;
                            chip.Update = ym3526.Update;
                            chip.Start = ym3526.Start;
                            chip.Stop = ym3526.Stop;
                            chip.Reset = ym3526.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.YM3526Volume;
                            chip.Clock = ((vgm)driver).YM3526ClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriOPL = 1;
                            else chipLED.SecOPL = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.YM3526 : EnmChip.S_YM3526);
                        }
                    }

                    if (vgmDriver.Y8950ClockValue != 0)
                    {
                        MDSound.y8950 y8950 = new MDSound.y8950();

                        for (int i = 0; i < (((vgm)driver).Y8950DualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.Y8950;
                            chip.ID = (byte)i;
                            chip.Instrument = y8950;
                            chip.Update = y8950.Update;
                            chip.Start = y8950.Start;
                            chip.Stop = y8950.Stop;
                            chip.Reset = y8950.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.Y8950Volume;
                            chip.Clock = ((vgm)driver).Y8950ClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriY8950 = 1;
                            else chipLED.SecY8950 = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.Y8950 : EnmChip.S_Y8950);
                        }
                    }

                    if (vgmDriver.DMGClockValue != 0)
                    {
                        MDSound.gb dmg = new MDSound.gb();

                        for (int i = 0; i < (((vgm)driver).DMGDualChipFlag ? 2 : 1); i++)
                        {
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.DMG;
                            chip.ID = (byte)i;
                            chip.Instrument = dmg;
                            chip.Update = dmg.Update;
                            chip.Start = dmg.Start;
                            chip.Stop = dmg.Stop;
                            chip.Reset = dmg.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.DMGVolume;
                            chip.Clock = ((vgm)driver).DMGClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriDMG = 1;
                            else chipLED.SecDMG = 1;

                            hiyorimiDeviceFlag |= 0x2;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.DMG : EnmChip.S_DMG);
                        }
                    }

                    if (vgmDriver.NESClockValue != 0)
                    {

                        for (int i = 0; i < (((vgm)driver).NESDualChipFlag ? 2 : 1); i++)
                        {
                            MDSound.nes_intf nes = new MDSound.nes_intf();
                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.Nes;
                            chip.ID = (byte)i;
                            chip.Instrument = nes;
                            chip.Update = nes.Update;
                            chip.Start = nes.Start;
                            chip.Stop = nes.Stop;
                            chip.Reset = nes.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.APUVolume;
                            chip.Clock = ((vgm)driver).NESClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriNES = 1;
                            else chipLED.SecNES = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.NES : EnmChip.S_NES);

                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.DMC;
                            chip.ID = (byte)i;
                            chip.Instrument = nes;
                            //chip.Update = nes.Update;
                            chip.Start = nes.Start;
                            chip.Stop = nes.Stop;
                            chip.Reset = nes.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.DMCVolume;
                            chip.Clock = ((vgm)driver).NESClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriDMC = 1;
                            else chipLED.SecDMC = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.DMC : EnmChip.S_DMC);


                            chip = new MDSound.MDSound.Chip();
                            chip.type = MDSound.MDSound.enmInstrumentType.FDS;
                            chip.ID = (byte)i;
                            chip.Instrument = nes;
                            //chip.Update = nes.Update;
                            chip.Start = nes.Start;
                            chip.Stop = nes.Stop;
                            chip.Reset = nes.Reset;
                            chip.SamplingRate = (UInt32)Common.SampleRate;
                            chip.Volume = setting.balance.FDSVolume;
                            chip.Clock = ((vgm)driver).NESClockValue;
                            chip.Option = null;
                            if (i == 0) chipLED.PriFDS = 1;
                            else chipLED.SecFDS = 1;

                            lstChips.Add(chip);
                            useChip.Add(i == 0 ? EnmChip.FDS : EnmChip.S_FDS);


                            hiyorimiDeviceFlag |= 0x2;

                        }
                    }
                }


                if (hiyorimiDeviceFlag == 0x3 && hiyorimiNecessary) hiyorimiNecessary = true;
                else hiyorimiNecessary = false;

                log.Write("MDSound 初期化");

                if (mds == null)
                    mds = new MDSound.MDSound((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());
                else
                    mds.Init((UInt32)Common.SampleRate, samplingBuffer, lstChips.ToArray());

                log.Write("ChipRegister 初期化");

                chipRegister.initChipRegister(lstChips.ToArray());

                if (setting.IsManualDetect)
                {
                    RealChipManualDetect(setting);
                }
                else
                {
                    RealChipAutoDetect(setting);
                }

                for (int i = 0; i < 2; i++)
                {
                    if (chipRegister.AY8910[i].Use)
                    {
                        if (chipRegister.AY8910[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.AY8910[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.C140[i].Use)
                    {
                        if (chipRegister.C140[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.C140[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.SEGAPCM[i].Use)
                    {
                        if (chipRegister.SEGAPCM[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.SEGAPCM[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.SN76489[i].Use)
                    {
                        if (chipRegister.SN76489[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.SN76489[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.YM2151[i].Use)
                    {
                        if (chipRegister.YM2151[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2151[i].Model == EnmModel.RealModel)
                        {
                            if (setting.YM2151Type.SoundLocation != -1)//GIMIC以外(SCCIの場合)
                            {
                                driver.SetYM2151Hosei(chipRegister.YM2151[i], vgmDriver.YM2151ClockValue);
                            }
                            useReal = true;
                        }
                    }

                    if (chipRegister.YM2203[i].Use)
                    {
                        if (chipRegister.YM2203[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2203[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.YM2413[i].Use)
                    {
                        if (chipRegister.YM2413[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2413[i].Model == EnmModel.RealModel) useReal = true;
                    }

                    if (chipRegister.YM2608[i].Use)
                    {
                        if (chipRegister.YM2608[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2608[i].Model == EnmModel.RealModel)
                        {
                            if (setting.YM2608Type.OnlyPCMEmulation) useEmu = true;
                            useReal = true;
                        }
                    }

                    if (chipRegister.YM2610[i].Use)
                    {
                        if (chipRegister.YM2610[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2610[i].Model == EnmModel.RealModel)
                        {
                            if (setting.YM2610Type.OnlyPCMEmulation) useEmu = true;
                            useReal = true;
                        }
                    }

                    if (chipRegister.YM2612[i].Use)
                    {
                        if (chipRegister.YM2612[i].Model == EnmModel.VirtualModel) useEmu = true;
                        if (chipRegister.YM2612[i].Model == EnmModel.RealModel)
                        {
                            if (setting.YM2612Type.OnlyPCMEmulation) useEmu = true;
                            useReal = true;
                        }
                    }

                }

                log.Write("Volume 設定");

                if (chipRegister.YM2203[0].Use || chipRegister.YM2203[1].Use)
                {
                    SetYM2203FMVolume(true, setting.balance.YM2203FMVolume);
                    SetYM2203PSGVolume(true, setting.balance.YM2203PSGVolume);
                }

                if (chipRegister.YM2608[0].Use || chipRegister.YM2608[1].Use)
                {
                    SetYM2608FMVolume(true, setting.balance.YM2608FMVolume);
                    SetYM2608PSGVolume(true, setting.balance.YM2608PSGVolume);
                    SetYM2608RhythmVolume(true, setting.balance.YM2608RhythmVolume);
                    SetYM2608AdpcmVolume(true, setting.balance.YM2608AdpcmVolume);
                }

                if (chipRegister.YM2610[0].Use || chipRegister.YM2610[1].Use)
                {
                    SetYM2610FMVolume(true, setting.balance.YM2610FMVolume);
                    SetYM2610PSGVolume(true, setting.balance.YM2610PSGVolume);
                    SetYM2610AdpcmAVolume(true, setting.balance.YM2610AdpcmAVolume);
                    SetYM2610AdpcmBVolume(true, setting.balance.YM2610AdpcmBVolume);
                }

                log.Write("Clock 設定");

                for (int i = 0; i < 2; i++)
                {
                    if (chipRegister.AY8910[i].Use) chipRegister.AY8910WriteClock((byte)i, (int)vgmDriver.AY8910ClockValue);
                    if (chipRegister.C140[i].Use)
                    {
                        chipRegister.C140WriteClock((byte)i, (int)vgmDriver.C140ClockValue);
                        chipRegister.C140WriteType(chipRegister.C140[i], vgmDriver.C140Type);
                    }
                    if (chipRegister.SEGAPCM[i].Use) chipRegister.SEGAPCMWriteClock((byte)i, (int)vgmDriver.SEGAPCMClockValue);
                    if (chipRegister.SN76489[i].Use) chipRegister.SN76489WriteClock((byte)i, (int)vgmDriver.SN76489ClockValue);
                    if (chipRegister.YM2151[i].Use) chipRegister.YM2151WriteClock((byte)i, (int)vgmDriver.YM2151ClockValue);
                    if (chipRegister.YM2203[i].Use) chipRegister.YM2203WriteClock((byte)i, (int)vgmDriver.YM2203ClockValue);
                    if (chipRegister.YM2413[i].Use) chipRegister.YM2413WriteClock((byte)i, (int)vgmDriver.YM2413ClockValue);
                    if (chipRegister.YM2608[i].Use) chipRegister.YM2608WriteClock((byte)i, (int)vgmDriver.YM2608ClockValue);
                    if (chipRegister.YM2612[i].Use) chipRegister.YM2612WriteClock((byte)i, (int)vgmDriver.YM2612ClockValue);

                }


                //
                log.Write("GIMIC向け SSGVolumeセット");
                //

                int SSGVolumeFromTAG = -1;
                SSGVolumeFromTAG = GetGIMICSSGVolumeFromTAG(vgmDriver.GD3.SystemNameJ);
                if (SSGVolumeFromTAG == -1)
                    SSGVolumeFromTAG = GetGIMICSSGVolumeFromTAG(vgmDriver.GD3.SystemName);
                if (SSGVolumeFromTAG == -1)
                    SSGVolumeFromTAG = setting.balance.GimicOPNVolume;

                for (int i = 0; i < 2; i++)
                {
                    if (chipRegister.YM2203[i].Use) chipRegister.YM2203SetSSGVolume((byte)i, SSGVolumeFromTAG);
                    if (chipRegister.YM2608[i].Use) chipRegister.YM2608SetSSGVolume((byte)i, SSGVolumeFromTAG);
                }


                PackData[] stopData = MakeSoftResetData();
                sm.SetStopData(stopData);

                Paused = false;
                //oneTimeReset = false;

                Thread.Sleep(100);

                //Stopped = false;

                log.Write("初期化完了");

                return true;
            }
            catch (Exception ex)
            {
                log.ForcedWrite(ex);
                return false;
            }

        }

        private static int GetGIMICSSGVolumeFromTAG(string tag)
        {
            if (tag.IndexOf("9801") > 0) return 31;
            if (tag.IndexOf("PC-98") > 0) return 31;
            if (tag.IndexOf("PC98") > 0) return 31;

            if (tag.IndexOf("8801") > 0) return 63;
            if (tag.IndexOf("PC-88") > 0) return 63;
            if (tag.IndexOf("PC88") > 0)  return 63;

            return -1;
        }


        private static void ResetFadeOutParam()
        {
            fadeoutCounter = 1.0;
            fadeoutCounterEmu = 1.0;
            fadeoutCounterDelta = 0.000004;
            vgmSpeed = 1;

            //chipRegister.YM2203SetFadeoutVolume(0, 0);
            //chipRegister.YM2203SetFadeoutVolume(1, 0);
            //chipRegister.setFadeoutVolYM2608(0,0, 0);
            //chipRegister.setFadeoutVolYM2608(0,1, 0);
            //chipRegister.YM2151SetFadeoutVolume(0, 0);
            //chipRegister.YM2151SetFadeoutVolume(1, 0);
            //chipRegister.setFadeoutVolYM2612(0, 0);
            //chipRegister.setFadeoutVolYM2612(1, 0);
            //chipRegister.setFadeoutVolSN76489(0, 0);
            //chipRegister.setFadeoutVolSN76489(1, 0);
            //chipRegister.resetChips();
        }

        public static void ChangeChipSampleRate(MDSound.MDSound.Chip chip, int NewSmplRate)
        {
            MDSound.MDSound.Chip CAA = chip;

            if (CAA.SamplingRate == NewSmplRate)
                return;

            // quick and dirty hack to make sample rate changes work
            CAA.SamplingRate = (uint)NewSmplRate;
            if (CAA.SamplingRate < Common.SampleRate)//SampleRate)
                CAA.Resampler = 0x01;
            else if (CAA.SamplingRate == Common.SampleRate)//SampleRate)
                CAA.Resampler = 0x02;
            else if (CAA.SamplingRate > Common.SampleRate)//SampleRate)
                CAA.Resampler = 0x03;
            CAA.SmpP = 1;
            CAA.SmpNext -= CAA.SmpLast;
            CAA.SmpLast = 0x00;

            return;
        }

        public static void FF()
        {
            vgmSpeed = (vgmSpeed == 1) ? 4 : 1;
            sm.SetSpeed(vgmSpeed);
            //driverReal.vgmSpeed = vgmSpeed;
        }

        public static void Slow()
        {
            vgmSpeed = (vgmSpeed == 1) ? 0.25 : 1;
            sm.SetSpeed(vgmSpeed);
            //driverReal.vgmSpeed = vgmSpeed;
        }

        public static void ResetSlow()
        {
            vgmSpeed = 1;
            driver.vgmSpeed = vgmSpeed;
            //driverReal.vgmSpeed = vgmSpeed;
        }

        public static void Pause()
        {

            try
            {
                Paused = !Paused;
                if (Paused)
                {
                    vgmSpeed = 0.0;
                    sm.SetSpeed(0.0);
                }
                else
                {
                    vgmSpeed = 1.0;
                    sm.SetSpeed(1.0);
                }
            }
            catch (Exception ex)
            {
                log.ForcedWrite(ex);
            }

        }

        public static bool isPaused
        {
            get
            {
                return Paused;
            }
        }

        public static bool isStopped
        {
            get
            {
                return Stopped;
            }
        }

        public static void StepPlay(int Step)
        {
            StepCounter = Step;
        }

        public static void Fadeout()
        {
            sm.SetFadeOut();
            //vgmFadeout = true;
        }

        public static void Stop()
        {

            sm.RequestStop();
            while (sm.IsRunningAsync())
            {
                Thread.Sleep(1);
                System.Windows.Forms.Application.DoEvents();
            }

            //try
            //{

            //    if (Stopped)
            //    {
            //        trdClosed = true;
            //        while (!trdStopped) { Thread.Sleep(1); };
            //        return;
            //    }

            //    if (!Paused)
            //    {
            //        NAudio.Wave.PlaybackState? ps = naudioWrap.GetPlaybackState();
            //        if (ps != null && ps != NAudio.Wave.PlaybackState.Stopped)
            //        {
            //            vgmFadeoutCounterV = 0.1;
            //            vgmFadeout = true;
            //            int cnt = 0;
            //            while (!Stopped && cnt < 100)
            //            {
            //                System.Threading.Thread.Sleep(1);
            //                System.Windows.Forms.Application.DoEvents();
            //                cnt++;
            //            }
            //        }
            //    }
            //    trdClosed = true;

            //    softReset(EnmModel.VirtualModel);
            //    softReset(EnmModel.RealModel);

            //    int timeout = 5000;
            //    while (!trdStopped)
            //    {
            //        Thread.Sleep(1);
            //        timeout--;
            //        if (timeout < 1) break;
            //    };
            //    while (!Stopped)
            //    {
            //        Thread.Sleep(1);
            //        timeout--;
            //        if (timeout < 1) break;
            //    };

            //    softReset(EnmModel.VirtualModel);
            //    softReset(EnmModel.RealModel);

            //    //chipRegister.outMIDIData_Close();
            //    Thread.Sleep(500);
            //    waveWriter.Close();

            //    //DEBUG
            //    //vstparse();
            //}
            //catch (Exception ex)
            //{
            //    log.ForcedWrite(ex);
            //}

        }

        public static void Close()
        {
            try
            {

                Stop();
                naudioWrap.Stop();

                //midi outをリリース
                //if (midiOuts.Count > 0)
                //{
                //    for (int i = 0; i < midiOuts.Count; i++)
                //    {
                //        if (midiOuts[i] != null)
                //        {
                //            try
                //            {
                //                //resetできない機種もある?
                //                midiOuts[i].Reset();
                //            }
                //            catch { }
                //            midiOuts[i].Close();
                //            midiOuts[i] = null;
                //        }
                //    }
                //    midiOuts.Clear();
                //    midiOutsType.Clear();
                //}

                //if (vstMidiOuts.Count > 0)
                //{
                //    vstMidiOuts.Clear();
                //    vstMidiOutsType.Clear();
                //}

                //setting.vst.VSTInfo = null;
                //List<vstInfo> vstlst = new List<vstInfo>();

                //for (int i = 0; i < vstPlugins.Count; i++)
                //{
                //    try
                //    {
                //        vstPlugins[i].vstPluginsForm.timer1.Enabled = false;
                //        vstPlugins[i].location = vstPlugins[i].vstPluginsForm.Location;
                //        vstPlugins[i].vstPluginsForm.Close();
                //    }
                //    catch { }

                //    try
                //    {
                //        if (vstPlugins[i].vstPlugins != null)
                //        {
                //            vstPlugins[i].vstPlugins.PluginCommandStub.EditorClose();
                //            vstPlugins[i].vstPlugins.PluginCommandStub.StopProcess();
                //            vstPlugins[i].vstPlugins.PluginCommandStub.MainsChanged(false);
                //            int pc = vstPlugins[i].vstPlugins.PluginInfo.ParameterCount;
                //            List<float> plst = new List<float>();
                //            for (int p = 0; p < pc; p++)
                //            {
                //                float v = vstPlugins[i].vstPlugins.PluginCommandStub.GetParameter(p);
                //                plst.Add(v);
                //            }
                //            vstPlugins[i].param = plst.ToArray();
                //            vstPlugins[i].vstPlugins.Dispose();
                //        }
                //    }
                //    catch { }

                //    vstInfo vi = new vstInfo();
                //    vi.editor = vstPlugins[i].editor;
                //    vi.fileName = vstPlugins[i].fileName;
                //    vi.key = vstPlugins[i].key;
                //    vi.effectName = vstPlugins[i].effectName;
                //    vi.power = vstPlugins[i].power;
                //    vi.location = vstPlugins[i].location;
                //    vi.param = vstPlugins[i].param;

                //    if (!vstPlugins[i].isInstrument) vstlst.Add(vi);
                //}
                //setting.vst.VSTInfo = vstlst.ToArray();


                //for (int i = 0; i < vstPluginsInst.Count; i++)
                //{
                //    try
                //    {
                //        vstPluginsInst[i].vstPluginsForm.timer1.Enabled = false;
                //        vstPluginsInst[i].location = vstPluginsInst[i].vstPluginsForm.Location;
                //        vstPluginsInst[i].vstPluginsForm.Close();
                //    }
                //    catch { }

                //    try
                //    {
                //        if (vstPluginsInst[i].vstPlugins != null)
                //        {
                //            vstPluginsInst[i].vstPlugins.PluginCommandStub.EditorClose();
                //            vstPluginsInst[i].vstPlugins.PluginCommandStub.StopProcess();
                //            vstPluginsInst[i].vstPlugins.PluginCommandStub.MainsChanged(false);
                //            int pc = vstPluginsInst[i].vstPlugins.PluginInfo.ParameterCount;
                //            List<float> plst = new List<float>();
                //            for (int p = 0; p < pc; p++)
                //            {
                //                float v = vstPluginsInst[i].vstPlugins.PluginCommandStub.GetParameter(p);
                //                plst.Add(v);
                //            }
                //            vstPluginsInst[i].param = plst.ToArray();
                //            vstPluginsInst[i].vstPlugins.Dispose();
                //        }
                //    }
                //    catch { }

                //    vstInfo vi = new vstInfo();
                //    vi.editor = vstPluginsInst[i].editor;
                //    vi.fileName = vstPluginsInst[i].fileName;
                //    vi.key = vstPluginsInst[i].key;
                //    vi.effectName = vstPluginsInst[i].effectName;
                //    vi.power = vstPluginsInst[i].power;
                //    vi.location = vstPluginsInst[i].location;
                //    vi.param = vstPluginsInst[i].param;

                //}

                //realChip.Close();

                sm.Release();
            }
            catch (Exception ex)
            {
                log.ForcedWrite(ex);
            }
        }

        public static long GetCounter()
        {
            //if (driverVirtual == null && driverReal == null) return -1;
            if (driver == null) return -1;

            //if (driverVirtual == null) return driverReal.Counter;
            //if (driverReal == null) return driverVirtual.Counter;

            //return driverVirtual.Counter > driverReal.Counter ? driverVirtual.Counter : driverReal.Counter;
            return sm.GetSeqCounter();
        }

        public static long GetTotalCounter()
        {
            if (driver == null) return -1;

            return driver.TotalCounter;
        }

        public static long GetDriverCounter()
        {
            //if (driverVirtual == null && driverReal == null) return -1;
            if (driver == null) return -1;


            //if (driverVirtual == null)
            //{
            //    if (driverReal is NRTDRV) return ((NRTDRV)driverReal).work.TOTALCOUNT;
            //    else if (driverReal is vgm) return ((vgm)driverReal).vgmFrameCounter;
            //    else return 0;
            //}
            //if (driverReal == null)
            //{
                //if (driver is NRTDRV) return ((NRTDRV)driver).work.TOTALCOUNT;
                //else
                if (driver is vgm) return ((vgm)driver).vgmFrameCounter;
                else return 0;
            //}

            //if (driverVirtual is NRTDRV && driverReal is NRTDRV)
            //{
            //    return ((NRTDRV)driverVirtual).work.TOTALCOUNT > ((NRTDRV)driverReal).work.TOTALCOUNT ? ((NRTDRV)driverVirtual).work.TOTALCOUNT : ((NRTDRV)driverReal).work.TOTALCOUNT;
            //}
            //else if (driverVirtual is vgm && driverReal is vgm)
            //{
            //    return ((vgm)driverVirtual).vgmFrameCounter > ((vgm)driverReal).vgmFrameCounter ? ((vgm)driverVirtual).vgmFrameCounter : ((vgm)driverReal).vgmFrameCounter;
            //}
            //else
            //{
            //    return 0;
            //}
        }

        public static long GetLoopCounter()
        {
            if (driver == null) return -1;

            return driver.LoopCounter;
        }

        public static byte[] GetChipStatus()
        {
            chips[0] = chipRegister.chipLED.PriOPN;
            chipRegister.chipLED.PriOPN = chipLED.PriOPN;
            chips[1] = chipRegister.chipLED.PriOPN2;
            chipRegister.chipLED.PriOPN2 = chipLED.PriOPN2;
            chips[2] = chipRegister.chipLED.PriOPNA;
            chipRegister.chipLED.PriOPNA = chipLED.PriOPNA;
            chips[3] = chipRegister.chipLED.PriOPNB;
            chipRegister.chipLED.PriOPNB = chipLED.PriOPNB;

            chips[4] = chipRegister.chipLED.PriOPM;
            chipRegister.chipLED.PriOPM = chipLED.PriOPM;
            chips[5] = chipRegister.chipLED.PriDCSG;
            chipRegister.chipLED.PriDCSG = chipLED.PriDCSG;
            chips[6] = chipRegister.chipLED.PriRF5C;
            chipRegister.chipLED.PriRF5C = chipLED.PriRF5C;
            chips[7] = chipRegister.chipLED.PriPWM;
            chipRegister.chipLED.PriPWM = chipLED.PriPWM;

            chips[8] = chipRegister.chipLED.PriOKI5;
            chipRegister.chipLED.PriOKI5 = chipLED.PriOKI5;
            chips[9] = chipRegister.chipLED.PriOKI9;
            chipRegister.chipLED.PriOKI9 = chipLED.PriOKI9;
            chips[10] = chipRegister.chipLED.PriC140;
            chipRegister.chipLED.PriC140 = chipLED.PriC140;
            chips[11] = chipRegister.chipLED.PriSPCM;
            chipRegister.chipLED.PriSPCM = chipLED.PriSPCM;

            chips[12] = chipRegister.chipLED.PriAY10;
            chipRegister.chipLED.PriAY10 = chipLED.PriAY10;
            chips[13] = chipRegister.chipLED.PriOPLL;
            chipRegister.chipLED.PriOPLL = chipLED.PriOPLL;
            chips[14] = chipRegister.chipLED.PriHuC;
            chipRegister.chipLED.PriHuC = chipLED.PriHuC;
            chips[15] = chipRegister.chipLED.PriC352;
            chipRegister.chipLED.PriC352 = chipLED.PriC352;
            chips[16] = chipRegister.chipLED.PriK054539;
            chipRegister.chipLED.PriK054539 = chipLED.PriK054539;


            chips[128 + 0] = chipRegister.chipLED.SecOPN;
            chipRegister.chipLED.SecOPN = chipLED.SecOPN;
            chips[128 + 1] = chipRegister.chipLED.SecOPN2;
            chipRegister.chipLED.SecOPN2 = chipLED.SecOPN2;
            chips[128 + 2] = chipRegister.chipLED.SecOPNA;
            chipRegister.chipLED.SecOPNA = chipLED.SecOPNA;
            chips[128 + 3] = chipRegister.chipLED.SecOPNB;
            chipRegister.chipLED.SecOPNB = chipLED.SecOPNB;

            chips[128 + 4] = chipRegister.chipLED.SecOPM;
            chipRegister.chipLED.SecOPM = chipLED.SecOPM;
            chips[128 + 5] = chipRegister.chipLED.SecDCSG;
            chipRegister.chipLED.SecDCSG = chipLED.SecDCSG;
            chips[128 + 6] = chipRegister.chipLED.SecRF5C;
            chipRegister.chipLED.SecRF5C = chipLED.SecRF5C;
            chips[128 + 7] = chipRegister.chipLED.SecPWM;
            chipRegister.chipLED.SecPWM = chipLED.SecPWM;

            chips[128 + 8] = chipRegister.chipLED.SecOKI5;
            chipRegister.chipLED.SecOKI5 = chipLED.SecOKI5;
            chips[128 + 9] = chipRegister.chipLED.SecOKI9;
            chipRegister.chipLED.SecOKI9 = chipLED.SecOKI9;
            chips[128 + 10] = chipRegister.chipLED.SecC140;
            chipRegister.chipLED.SecC140 = chipLED.SecC140;
            chips[128 + 11] = chipRegister.chipLED.SecSPCM;
            chipRegister.chipLED.SecSPCM = chipLED.SecSPCM;

            chips[128 + 12] = chipRegister.chipLED.SecAY10;
            chipRegister.chipLED.SecAY10 = chipLED.SecAY10;
            chips[128 + 13] = chipRegister.chipLED.SecOPLL;
            chipRegister.chipLED.SecOPLL = chipLED.SecOPLL;
            chips[128 + 14] = chipRegister.chipLED.SecHuC;
            chipRegister.chipLED.SecHuC = chipLED.SecHuC;
            chips[128 + 15] = chipRegister.chipLED.SecC352;
            chipRegister.chipLED.SecC352 = chipLED.SecC352;
            chips[128 + 16] = chipRegister.chipLED.SecK054539;
            chipRegister.chipLED.SecK054539 = chipLED.SecK054539;


            return chips;
        }

        public static void updateVol()
        {
            chipRegister.updateVol();
        }

        public static uint GetVgmCurLoopCounter()
        {
            uint cnt = 0;

            if (driver != null)
            {
                cnt = driver.vgmCurLoop;
            }
            //if (driverReal != null)
            //{
            //    cnt = Math.Min(driverReal.vgmCurLoop, cnt);
            //}

            return cnt;
        }

        public static bool GetVGMStopped()
        {
            bool v;
            //bool r;

            v = driver == null ? true : driver.Stopped;
            //r = driverReal == null ? true : driverReal.Stopped;
            return v;// && r;
        }

        public static bool GetIsDataBlock()
        {
            if (sm == null) return false;
            return sm.GetInterrupt();
        }



        private static void NaudioWrap_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                System.Windows.Forms.MessageBox.Show(
                    string.Format("デバイスが何らかの原因で停止しました。\r\nメッセージ:\r\n{0}", e.Exception.Message)
                    , "エラー"
                    , System.Windows.Forms.MessageBoxButtons.OK
                    , System.Windows.Forms.MessageBoxIcon.Error);
                flgReinit = true;

                try
                {
                    naudioWrap.Stop();
                }
                catch (Exception ex)
                {
                    log.ForcedWrite(ex);
                }

            }
            else
            {
                try
                {
                    Stop();
                }
                catch { }
            }
        }

        //private static void startTrdVgmReal()
        //{
        //    if (setting.outputDevice.DeviceType == Common.DEV_Null)
        //    {
        //        return;
        //    }

        //    trdClosed = false;
        //    trdMain = new Thread(new ThreadStart(trdVgmRealFunction));
        //    trdMain.Priority = ThreadPriority.Highest;
        //    trdMain.IsBackground = true;
        //    trdMain.Name = "trdVgmReal";
        //    trdMain.Start();
        //}

        //private static void trdVgmRealFunction()
        //{
        //    double o = sw.ElapsedTicks / swFreq;
        //    double step = 1 / (double)Common.SampleRate;

        //    trdStopped = false;
        //    try
        //    {
        //        while (!trdClosed)
        //        {
        //            Thread.Sleep(0);

        //            double el1 = sw.ElapsedTicks / swFreq;
        //            if (el1 - o < step) continue;
        //            if (el1 - o >= step * Common.SampleRate / 100.0)//閾値10ms
        //            {
        //                do
        //                {
        //                    o += step;
        //                } while (el1 - o >= step);
        //            }
        //            else
        //            {
        //                o += step;
        //            }

        //            if (Stopped || Paused)
        //            {
        //                if (realChip != null && !oneTimeReset)
        //                {
        //                    softReset(EnmModel.RealModel);
        //                    oneTimeReset = true;
        //                    chipRegister.resetAllMIDIout();
        //                }
        //                continue;
        //            }
        //            if (hiyorimiNecessary && driverVirtual.isDataBlock) { continue; }

        //            if (vgmFadeout)
        //            {
        //                if (vgmRealFadeoutVol != 1000) vgmRealFadeoutVolWait--;
        //                if (vgmRealFadeoutVolWait == 0)
        //                {
        //                    if (useChip.Contains(EnmChip.YM2151)) chipRegister.setFadeoutVolYM2151(0, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.YM2203)) chipRegister.setFadeoutVolYM2203(0, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.YM2608)) chipRegister.setFadeoutVolYM2608(0, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.YM2610)) chipRegister.setFadeoutVolYM2610(0, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.YM2612)) chipRegister.setFadeoutVolYM2612(0, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.SN76489)) chipRegister.setFadeoutVolSN76489(0, vgmRealFadeoutVol);

        //                    if (useChip.Contains(EnmChip.S_YM2151)) chipRegister.setFadeoutVolYM2151(1, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.S_YM2203)) chipRegister.setFadeoutVolYM2203(1, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.S_YM2608)) chipRegister.setFadeoutVolYM2608(1, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.S_YM2610)) chipRegister.setFadeoutVolYM2610(1, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.S_YM2612)) chipRegister.setFadeoutVolYM2612(1, vgmRealFadeoutVol);
        //                    if (useChip.Contains(EnmChip.S_SN76489)) chipRegister.setFadeoutVolSN76489(1, vgmRealFadeoutVol);

        //                    vgmRealFadeoutVol++;

        //                    vgmRealFadeoutVol = Math.Min(127, vgmRealFadeoutVol);
        //                    if (vgmRealFadeoutVol == 127)
        //                    {
        //                        if (realChip != null)
        //                        {
        //                            softReset(EnmModel.RealModel);
        //                        }
        //                        vgmRealFadeoutVolWait = 1000;
        //                        chipRegister.resetAllMIDIout();
        //                    }
        //                    else
        //                    {
        //                        vgmRealFadeoutVolWait = 700 - vgmRealFadeoutVol * 2;
        //                    }
        //                }
        //            }

        //            if (hiyorimiNecessary)
        //            {
        //                //long v;
        //                //v = driverReal.vgmFrameCounter - driverVirtual.vgmFrameCounter;
        //                //long d = common.SampleRate * (setting.LatencySCCI - common.SampleRate * setting.LatencyEmulation) / 1000;
        //                //long l = getLatency() / 4;

        //                //int m = 0;
        //                //if (d >= 0)
        //                //{
        //                //    if (v >= d - l && v <= d + l) m = 0;
        //                //    else m = (v + d > l) ? 1 : 2;
        //                //}
        //                //else
        //                //{
        //                //    d = Math.Abs(common.SampleRate * ((uint)setting.LatencyEmulation - (uint)setting.LatencySCCI) / 1000);
        //                //    if (v >= d - l && v <= d + l) m = 0;
        //                //    else m = (v - d > l) ? 1 : 2;
        //                //}

        //                double dEMU = Common.SampleRate * setting.LatencyEmulation / 1000.0;
        //                double dSCCI = Common.SampleRate * setting.LatencySCCI / 1000.0;
        //                double abs = Math.Abs((driverReal.vgmFrameCounter - dSCCI) - (driverVirtual.vgmFrameCounter - dEMU));
        //                int m = 0;
        //                long l = getLatency() / 10;
        //                if (abs >= l)
        //                {
        //                    m = ((driverReal.vgmFrameCounter - dSCCI) > (driverVirtual.vgmFrameCounter - dEMU)) ? 1 : 2;
        //                }

        //                switch (m)
        //                {
        //                    case 0: //x1
        //                        driverReal.oneFrameProc();
        //                        break;
        //                    case 1: //x1/2
        //                        hiyorimiEven++;
        //                        if (hiyorimiEven > 1)
        //                        {
        //                            driverReal.oneFrameProc();
        //                            hiyorimiEven = 0;
        //                        }
        //                        break;
        //                    case 2: //x2
        //                        driverReal.oneFrameProc();
        //                        driverReal.oneFrameProc();
        //                        break;
        //                }
        //            }
        //            else
        //            {
        //                driverReal.oneFrameProc();
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //    trdStopped = true;
        //}

        private static void softReset(long counter)
        {
            for (int i = 0; i < 2; i++)
            {
                if (chipRegister.AY8910[i].Use) chipRegister.AY8910SoftReset(counter, i);
                if (chipRegister.C140[i].Use) chipRegister.C140SoftReset(counter, i);
                if (chipRegister.SEGAPCM[i].Use) chipRegister.SEGAPCMSoftReset(counter, i);
                if (chipRegister.SN76489[i].Use) chipRegister.SN76489SoftReset(counter, i);
                if (chipRegister.YM2151[i].Use) chipRegister.YM2151SoftReset(counter, i);
                if (chipRegister.YM2203[i].Use) chipRegister.YM2203SoftReset(counter, i);
                if (chipRegister.YM2413[i].Use) chipRegister.YM2413SoftReset(counter, i);
                if (chipRegister.YM2608[i].Use) chipRegister.YM2608SoftReset(counter, i);
                if (chipRegister.YM2610[i].Use) chipRegister.YM2610SoftReset(counter, i);
                if (chipRegister.YM2612[i].Use) chipRegister.YM2612SoftReset(counter, i);
            }
            //for (int i = 0; i < midiOuts.Count; i++)
            //{
                //chipRegister.MIDISoftReset(counter, i);
            //}
        }

        private static PackData[] MakeSoftResetData()
        {
            List<PackData> data = new List<PackData>();
            for (int i = 0; i < 2; i++)
            {
                if (chipRegister.AY8910[i].Use) data.AddRange(chipRegister.AY8910MakeSoftReset(i));
                if (chipRegister.C140[i].Use) data.AddRange(chipRegister.C140MakeSoftReset(i));
                if (chipRegister.SEGAPCM[i].Use) data.AddRange(chipRegister.SEGAPCMMakeSoftReset(i));
                if (chipRegister.SN76489[i].Use) data.AddRange(chipRegister.SN76489MakeSoftReset(i));
                if (chipRegister.YM2151[i].Use) data.AddRange(chipRegister.YM2151MakeSoftReset(i));
                if (chipRegister.YM2203[i].Use) data.AddRange(chipRegister.YM2203MakeSoftReset(i));
                if (chipRegister.YM2413[i].Use) data.AddRange(chipRegister.YM2413MakeSoftReset(i));
                if (chipRegister.YM2608[i].Use) data.AddRange(chipRegister.YM2608MakeSoftReset(i));
                if (chipRegister.YM2610[i].Use) data.AddRange(chipRegister.YM2610MakeSoftReset(i));
                if (chipRegister.YM2612[i].Use) data.AddRange(chipRegister.YM2612MakeSoftReset(i));
            }

            //for (int i = 0; i < midiOuts.Count; i++)
            //{
                //data.AddRange(chipRegister.MIDIMakeSoftReset(i));
            //}

            return data.ToArray();
        }


        public static int trdVgmVirtualFunction(short[] buffer, int offset, int sampleCount)
        {
            int cnt = trdVgmVirtualMainFunction(buffer, offset, sampleCount);

            if (setting.midiKbd.UseMIDIKeyboard)
            {
                if (bufVirtualFunction_MIDIKeyboard == null || bufVirtualFunction_MIDIKeyboard.Length < sampleCount)
                {
                    bufVirtualFunction_MIDIKeyboard = new short[sampleCount];
                }
                mdsMIDI.Update(bufVirtualFunction_MIDIKeyboard, 0, sampleCount, null);
                for (int i = 0; i < sampleCount; i++)
                {
                    buffer[i + offset] += bufVirtualFunction_MIDIKeyboard[i];
                }
            }
            return cnt;
        }

        private static long PackCounter = 0;
        private static SoundManager.PackData Pack = new SoundManager.PackData();

        private static bool AudioDeviceSync = false;
        private static object lockObjAudioDeviceSync = new object();
        public static bool GetAudioDeviceSync()
        {
            lock (lockObjAudioDeviceSync)
            {
                return AudioDeviceSync;
            }
        }
        public static void SetAudioDeviceSync()
        {
            lock (lockObjAudioDeviceSync)
            {
                AudioDeviceSync = true;
            }
        }
        public static void ResetAudioDeviceSync()
        {
            lock (lockObjAudioDeviceSync)
            {
                AudioDeviceSync = false;
            }
        }

        private static int trdVgmVirtualMainFunction(short[] buffer, int offset, int sampleCount)
        {
            EmuSampleCount = sampleCount;

            if (buffer == null || buffer.Length < 1 || sampleCount == 0)
            {
                SetAudioDeviceSync();
                return sampleCount;
            }

            if (sm == null || !GetAudioDeviceSync())
            {
                SetAudioDeviceSync();
                return sampleCount;
            }

            try
            {
                //stwh.Reset(); stwh.Start();

                int i;
                int cnt = 0;

                //if (!sm.IsRunningAsync())
                //{
                //Stopped = true;
                //}

                //if (Stopped || Paused) return mds.Update(buffer, offset, sampleCount, null);

                long bufCnt = sampleCount / 2;
                long seqcnt = sm.GetSeqCounter();

                EmuSeqCounterDelta = sm.GetSpeed();// 1.0;

                //スピードの調整はせずにディレイの調整を行う
                long sub = (seqcnt - EmuSeqCounter);
                if (Math.Abs(sub) > bufCnt)
                {
                    long delta = Math.Abs(sub) - bufCnt;
                    if (Math.Sign(delta) > 0)
                    {

                    }
                    else
                    {

                    }
                }

                //スピードの調整をする場合は以下を有効にする(通常、調整なし)
                {
                    //EmuSeqCounterDelta = (seqcnt - EmuSeqCounter) / (double)bufCnt;
                    //EmuSeqCounterDelta = Math.Max(Math.Min(EmuSeqCounterDelta, 2.0), 0.5);
                    //RealSeqCounterDelta = 1.0 / EmuSeqCounterDelta;// (EmuSeqCounter- seqcnt) / (double)bufCnt;
                    //RealSeqCounterDelta = Math.Max(Math.Min(RealSeqCounterDelta, 2.0), 0.5);
                    //EmuSeqCounterDelta = 1.0;
                    //sm.SetSpeed(RealSeqCounterDelta);
                }

                if (bufCnt > getLatency()*2)
                {
                    ;
                }
                //EmuSeqCounter = Math.Max(EmuSeqCounter, 0);
                //if (!sm.IsRunningAtEmuChipSender()) EmuSeqCounter = 0;
                callcount = 0;

                //if (driver is nsf)
                //{
                //    driver.vstDelta = 0;
                //    cnt = (Int32)((nsf)driver).Render(buffer, (UInt32)sampleCount / 2, offset) * 2;
                //}
                //else if (driver is Driver.SID.sid)
                //{
                //    driver.vstDelta = 0;
                //    cnt = (Int32)((Driver.SID.sid)driver).Render(buffer, (UInt32)sampleCount);
                //}
                //else if (driver is Driver.MXDRV.MXDRV)
                //{
                //    mds.setIncFlag();
                //    driver.vstDelta = 0;
                //    for (i = 0; i < sampleCount; i += 2)
                //    {
                //        cnt = (Int32)((Driver.MXDRV.MXDRV)driver).Render(buffer, offset + i, 2);
                //        mds.Update(buffer, offset + i, 2, null);
                //    }
                //    //cnt = (Int32)((Driver.MXDRV.MXDRV)driverVirtual).Render(buffer, offset , sampleCount);
                //    //mds.Update(buffer, offset , sampleCount, null);
                //    cnt = sampleCount;
                //}
                //else
                {
                    //if (hiyorimiNecessary)// && driverReal.isDataBlock)
                    //{
                    //    mds.Update(buffer, offset, sampleCount, null);
                    //    SetAudioDeviceSync();
                    //    return sampleCount;
                    //}

                    if (StepCounter > 0)
                    {
                        StepCounter -= sampleCount;
                        if (StepCounter <= 0)
                        {
                            Paused = true;
                            StepCounter = 0;
                            mds.Update(buffer, offset, sampleCount, null);
                            SetAudioDeviceSync();
                            return sampleCount;
                        }
                    }

                    if(driver!=null) driver.vstDelta = 0;
                    stwh.Reset(); stwh.Start();
                    cnt = mds.Update(buffer, offset, sampleCount, oneFrameEmuDataSend);// driverVirtual.oneFrameProc);
                    ProcTimePer1Frame = ((double)stwh.ElapsedMilliseconds / (sampleCount + 1) * 1000000.0);
                }

                //if (callcount > bufCnt)
                //{
                //    ;
                //}

                //VST
                //if (vstPlugins.Count > 0 || vstPluginsInst.Count > 0) VST_Update(buffer, offset, sampleCount);

                for (i = 0; i < sampleCount; i++)
                {
                    int mul = (int)(16384.0 * Math.Pow(10.0, MasterVolume / 40.0));
                    buffer[offset + i] = (short)Common.Range((buffer[offset + i] * mul) >> 14, -0x8000, 0x7fff);

                    if (!sm.GetFadeOut()) continue;

                    //フェードアウト処理
                    buffer[offset + i] = (short)(buffer[offset + i] * fadeoutCounterEmu);
                    fadeoutCounterEmu -= fadeoutCounterDelta;
                    if (fadeoutCounterEmu <= 0.0)
                    {
                        fadeoutCounterEmu = 0.0;
                    }

                }

                if (setting.outputDevice.DeviceType != Common.DEV_Null)
                {
                    updateVisualVolume(buffer, offset);
                }

                //waveWriter.Write(buffer, offset, sampleCount);

                ////1frame当たりの処理時間
                //ProcTimePer1Frame = (int)((double)stwh.ElapsedMilliseconds / sampleCount * 1000000.0);
                SetAudioDeviceSync();
                return sampleCount;

            }
            catch (Exception ex)
            {
                log.ForcedWrite(ex);
                fatalError = true;
                Stopped = true;
            }

            SetAudioDeviceSync();
            return sampleCount;
        }

        public static double EmuSeqCounterDelta = 0.0;
        public static double RealSeqCounterDelta = 0.0;
        static double EmuSeqCounterWDelta =0.0;
        static int callcount = 0;
        private static bool useEmu;
        private static bool useReal;
        public static int EmuSampleCount;

        private static void oneFrameEmuDataSend()
        {
            if (emuRecvBuffer == null) return;

            while ((long)emuRecvBuffer.LookUpCounter() <= EmuSeqCounter || !sm.IsRunningAtDataSender())//&& recvBuffer.LookUpCounter() != 0)
            {
                if (sm.IsRunningAtDataSender() && EmuSeqCounter > sm.GetSeqCounter())
                {
                    return;
                }

                bool ret = emuRecvBuffer.Deq(ref PackCounter, ref Pack.Chip, ref Pack.Type, ref Pack.Address, ref Pack.Data, ref Pack.ExData);
                if (!ret)
                {
                    if (!sm.IsRunningAtDataSender())
                    {
                        sm.RequestStopAtEmuChipSender();
                    }
                    break;
                }
                if(EmuSeqCounter- PackCounter > 5)
                {
                    ;
                }
                chipRegister.SendChipData(PackCounter, Pack.Chip, Pack.Type, Pack.Address, Pack.Data, Pack.ExData);
                //log.Write(PackCounter.ToString());
            }

            while (EmuSeqCounterWDelta >= 1.0)
            {
                if (sm.IsRunningAsync() && sm.IsRunningAtEmuChipSender())
                {
                    EmuSeqCounter++;
                }
                EmuSeqCounterWDelta -= 1.0;
            }
            EmuSeqCounterWDelta += EmuSeqCounterDelta;
            //EmuSeqCounterWDelta += (sm.IsRunningAtEmuChipSender()) ? EmuSeqCounterDelta : 0;
            callcount++;
        }

        private static void updateVisualVolume(short[] buffer, int offset)
        {
            visVolume.master = buffer[offset];

            int[][][] vol = mds.getYM2151VisVolume();
            if (vol != null) visVolume.ym2151 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYM2203VisVolume();
            if (vol != null) visVolume.ym2203 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (vol != null) visVolume.ym2203FM = (short)getMonoVolume(vol[0][1][0], vol[0][1][1], vol[1][1][0], vol[1][1][1]);
            if (vol != null) visVolume.ym2203SSG = (short)getMonoVolume(vol[0][2][0], vol[0][2][1], vol[1][2][0], vol[1][2][1]);

            vol = mds.getYM2612VisVolume();
            if (vol != null) visVolume.ym2612 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYM2608VisVolume();
            if (vol != null) visVolume.ym2608 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (vol != null) visVolume.ym2608FM = (short)getMonoVolume(vol[0][1][0], vol[0][1][1], vol[1][1][0], vol[1][1][1]);
            if (vol != null) visVolume.ym2608SSG = (short)getMonoVolume(vol[0][2][0], vol[0][2][1], vol[1][2][0], vol[1][2][1]);
            if (vol != null) visVolume.ym2608Rtm = (short)getMonoVolume(vol[0][3][0], vol[0][3][1], vol[1][3][0], vol[1][3][1]);
            if (vol != null) visVolume.ym2608APCM = (short)getMonoVolume(vol[0][4][0], vol[0][4][1], vol[1][4][0], vol[1][4][1]);

            vol = mds.getYM2610VisVolume();
            if (vol != null) visVolume.ym2610 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (vol != null) visVolume.ym2610FM = (short)getMonoVolume(vol[0][1][0], vol[0][1][1], vol[1][1][0], vol[1][1][1]);
            if (vol != null) visVolume.ym2610SSG = (short)getMonoVolume(vol[0][2][0], vol[0][2][1], vol[1][2][0], vol[1][2][1]);
            if (vol != null) visVolume.ym2610APCMA = (short)getMonoVolume(vol[0][3][0], vol[0][3][1], vol[1][3][0], vol[1][3][1]);
            if (vol != null) visVolume.ym2610APCMB = (short)getMonoVolume(vol[0][4][0], vol[0][4][1], vol[1][4][0], vol[1][4][1]);


            vol = mds.getYM2413VisVolume();
            if (vol != null) visVolume.ym2413 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYM3526VisVolume();
            if (vol != null) visVolume.ym3526 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getY8950VisVolume();
            if (vol != null) visVolume.y8950 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYM3812VisVolume();
            if (vol != null) visVolume.ym3812 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYMF262VisVolume();
            if (vol != null) visVolume.ymf262 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYMF278BVisVolume();
            if (vol != null) visVolume.ymf278b = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYMF271VisVolume();
            if (vol != null) visVolume.ymf271 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getYMZ280BVisVolume();
            if (vol != null) visVolume.ymz280b = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getAY8910VisVolume();
            if (vol != null) visVolume.ay8910 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getSN76489VisVolume();
            if (vol != null) visVolume.sn76489 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getHuC6280VisVolume();
            if (vol != null) visVolume.huc6280 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);


            vol = mds.getRF5C164VisVolume();
            if (vol != null) visVolume.rf5c164 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getRF5C68VisVolume();
            if (vol != null) visVolume.rf5c68 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getPWMVisVolume();
            if (vol != null) visVolume.pwm = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getOKIM6258VisVolume();
            if (vol != null) visVolume.okim6258 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getOKIM6295VisVolume();
            if (vol != null) visVolume.okim6295 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getC140VisVolume();
            if (vol != null) visVolume.c140 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getC352VisVolume();
            if (vol != null) visVolume.c352 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getSegaPCMVisVolume();
            if (vol != null) visVolume.segaPCM = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getMultiPCMVisVolume();
            if (vol != null) visVolume.multiPCM = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getK051649VisVolume();
            if (vol != null) visVolume.k051649 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getK053260VisVolume();
            if (vol != null) visVolume.k053260 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getK054539VisVolume();
            if (vol != null) visVolume.k054539 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getQSoundVisVolume();
            if (vol != null) visVolume.qSound = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);

            vol = mds.getGA20VisVolume();
            if (vol != null) visVolume.ga20 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);


            vol = mds.getNESVisVolume();
            if (vol != null) visVolume.APU = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            else visVolume.APU = (short)MDSound.MDSound.np_nes_apu_volume;

            vol = mds.getDMCVisVolume();
            if (vol != null) visVolume.DMC = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            else visVolume.DMC = (short)MDSound.MDSound.np_nes_dmc_volume;

            vol = mds.getFDSVisVolume();
            if (vol != null) visVolume.FDS = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            else visVolume.FDS = (short)MDSound.MDSound.np_nes_fds_volume;

            vol = mds.getMMC5VisVolume();
            if (vol != null) visVolume.MMC5 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (visVolume.MMC5 == 0) visVolume.MMC5 = (short)MDSound.MDSound.np_nes_mmc5_volume;

            vol = mds.getN160VisVolume();
            if (vol != null) visVolume.N160 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (visVolume.N160 == 0) visVolume.N160 = (short)MDSound.MDSound.np_nes_n106_volume;

            vol = mds.getVRC6VisVolume();
            if (vol != null) visVolume.VRC6 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (visVolume.VRC6 == 0) visVolume.VRC6 = (short)MDSound.MDSound.np_nes_vrc6_volume;

            vol = mds.getVRC7VisVolume();
            if (vol != null) visVolume.VRC7 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (visVolume.VRC7 == 0) visVolume.VRC7 = (short)MDSound.MDSound.np_nes_vrc7_volume;

            vol = mds.getFME7VisVolume();
            if (vol != null) visVolume.FME7 = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
            if (visVolume.FME7 == 0) visVolume.FME7 = (short)MDSound.MDSound.np_nes_fme7_volume;

            vol = mds.getDMGVisVolume();
            if (vol != null) visVolume.DMG = (short)getMonoVolume(vol[0][0][0], vol[0][0][1], vol[1][0][0], vol[1][0][1]);
        }

        public static int getMonoVolume(int pl, int pr, int sl, int sr)
        {
            int v = pl + pr + sl + sr;
            v >>= 1;
            if (sl + sr != 0) v >>= 1;

            return v;
        }

        public static long getVirtualFrameCounter()
        {
            if (driver == null) return -1;
            return driver.vgmFrameCounter;
        }

        public static long getRealFrameCounter()
        {
            return -1;
            //if (driverReal == null) return -1;
            //return driverReal.vgmFrameCounter;
        }

        public static GD3 GetGD3()
        {
            if (driver != null) return driver.GD3;
            return null;
        }

        //private static VstPluginContext OpenPlugin(string pluginPath)
        //{
        //    try
        //    {
        //        HostCommandStub hostCmdStub = new HostCommandStub();
        //        hostCmdStub.PluginCalled += new EventHandler<PluginCalledEventArgs>(HostCmdStub_PluginCalled);

        //        VstPluginContext ctx = VstPluginContext.Create(pluginPath, hostCmdStub);

        //        // add custom data to the context
        //        ctx.Set("PluginPath", pluginPath);
        //        ctx.Set("HostCmdStub", hostCmdStub);

        //        // actually open the plugin itself
        //        ctx.PluginCommandStub.Open();

        //        return ctx;
        //    }
        //    catch (Exception e)
        //    {
        //        log.ForcedWrite(e);
        //        //MessageBox.Show(this, e.ToString(), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }

        //    return null;
        //}

        //private static void ReleaseAllPlugins()
        //{
        //    foreach (vstInfo2 ctx in vstPlugins)
        //    {
        //        // dispose of all (unmanaged) resources
        //        ctx.vstPlugins.Dispose();
        //    }

        //    vstPlugins.Clear();
        //}

        //private static void HostCmdStub_PluginCalled(object sender, PluginCalledEventArgs e)
        //{
        //    HostCommandStub hostCmdStub = (HostCommandStub)sender;

        //    // can be null when called from inside the plugin main entry point.
        //    if (hostCmdStub.PluginContext.PluginInfo != null)
        //    {
        //        Debug.WriteLine("Plugin " + hostCmdStub.PluginContext.PluginInfo.PluginID + " called:" + e.Message);
        //    }
        //    else
        //    {
        //        Debug.WriteLine("The loading Plugin called:" + e.Message);
        //    }
        //}



        //private static void VST_Update(short[] buffer, int offset, int sampleCount)
        //{
        //    if (buffer == null || buffer.Length < 1 || sampleCount == 0) return;

        //    try
        //    {
        //        //if (trdStopped) return;

        //        int blockSize = sampleCount / 2;

        //        for (int i = 0; i < vstPluginsInst.Count; i++)
        //        {
        //            vstInfo2 info2 = vstPluginsInst[i];
        //            VstPluginContext PluginContext = info2.vstPlugins;
        //            if (PluginContext == null) continue;
        //            if (PluginContext.PluginCommandStub == null) continue;


        //            int inputCount = info2.vstPlugins.PluginInfo.AudioInputCount;
        //            int outputCount = info2.vstPlugins.PluginInfo.AudioOutputCount;

        //            using (VstAudioBufferManager inputMgr = new VstAudioBufferManager(inputCount, blockSize))
        //            {
        //                using (VstAudioBufferManager outputMgr = new VstAudioBufferManager(outputCount, blockSize))
        //                {
        //                    VstAudioBuffer[] inputBuffers = inputMgr.ToArray();
        //                    VstAudioBuffer[] outputBuffers = outputMgr.ToArray();

        //                    if (inputCount != 0)
        //                    {
        //                        inputMgr.ClearBuffer(inputBuffers[0]);
        //                        inputMgr.ClearBuffer(inputBuffers[1]);

        //                        for (int j = 0; j < blockSize; j++)
        //                        {
        //                            // generate a value between -1.0 and 1.0
        //                            inputBuffers[0][j] = buffer[j * 2 + offset + 0] / (float)short.MaxValue;
        //                            inputBuffers[1][j] = buffer[j * 2 + offset + 1] / (float)short.MaxValue;
        //                        }
        //                    }

        //                    outputMgr.ClearBuffer(outputBuffers[0]);
        //                    outputMgr.ClearBuffer(outputBuffers[1]);

        //                    PluginContext.PluginCommandStub.ProcessEvents(info2.lstEvent.ToArray());
        //                    info2.lstEvent.Clear();


        //                    PluginContext.PluginCommandStub.ProcessReplacing(inputBuffers, outputBuffers);

        //                    for (int j = 0; j < blockSize; j++)
        //                    {
        //                        // generate a value between -1.0 and 1.0
        //                        if (inputCount == 0)
        //                        {
        //                            buffer[j * 2 + offset + 0] += (short)(outputBuffers[0][j] * short.MaxValue);
        //                            buffer[j * 2 + offset + 1] += (short)(outputBuffers[1][j] * short.MaxValue);
        //                        }
        //                        else
        //                        {
        //                            buffer[j * 2 + offset + 0] = (short)(outputBuffers[0][j] * short.MaxValue);
        //                            buffer[j * 2 + offset + 1] = (short)(outputBuffers[1][j] * short.MaxValue);
        //                        }
        //                    }

        //                }
        //            }
        //        }

        //        for (int i = 0; i < vstPlugins.Count; i++)
        //        {
        //            vstInfo2 info2 = vstPlugins[i];
        //            VstPluginContext PluginContext = info2.vstPlugins;
        //            if (PluginContext == null) continue;
        //            if (PluginContext.PluginCommandStub == null) continue;


        //            int inputCount = info2.vstPlugins.PluginInfo.AudioInputCount;
        //            int outputCount = info2.vstPlugins.PluginInfo.AudioOutputCount;

        //            using (VstAudioBufferManager inputMgr = new VstAudioBufferManager(inputCount, blockSize))
        //            {
        //                using (VstAudioBufferManager outputMgr = new VstAudioBufferManager(outputCount, blockSize))
        //                {
        //                    VstAudioBuffer[] inputBuffers = inputMgr.ToArray();
        //                    VstAudioBuffer[] outputBuffers = outputMgr.ToArray();

        //                    if (inputCount != 0)
        //                    {
        //                        inputMgr.ClearBuffer(inputBuffers[0]);
        //                        inputMgr.ClearBuffer(inputBuffers[1]);

        //                        for (int j = 0; j < blockSize; j++)
        //                        {
        //                            // generate a value between -1.0 and 1.0
        //                            inputBuffers[0][j] = buffer[j * 2 + offset + 0] / (float)short.MaxValue;
        //                            inputBuffers[1][j] = buffer[j * 2 + offset + 1] / (float)short.MaxValue;
        //                        }
        //                    }

        //                    outputMgr.ClearBuffer(outputBuffers[0]);
        //                    outputMgr.ClearBuffer(outputBuffers[1]);

        //                    PluginContext.PluginCommandStub.ProcessReplacing(inputBuffers, outputBuffers);

        //                    for (int j = 0; j < blockSize; j++)
        //                    {
        //                        // generate a value between -1.0 and 1.0
        //                        if (inputCount == 0)
        //                        {
        //                            buffer[j * 2 + offset + 0] += (short)(outputBuffers[0][j] * short.MaxValue);
        //                            buffer[j * 2 + offset + 1] += (short)(outputBuffers[1][j] * short.MaxValue);
        //                        }
        //                        else
        //                        {
        //                            buffer[j * 2 + offset + 0] = (short)(outputBuffers[0][j] * short.MaxValue);
        //                            buffer[j * 2 + offset + 1] = (short)(outputBuffers[1][j] * short.MaxValue);
        //                        }
        //                    }

        //                }
        //            }
        //        }

        //    }
        //    catch { }
        //}

        //private static void vstparse()
        //{
        //    while (vstPluginsInst.Count > 0)
        //    {
        //        if (vstPluginsInst[0] != null)
        //        {
        //            if (vstPluginsInst[0].vstPlugins.PluginCommandStub != null) vstPluginsInst[0].vstPlugins.PluginCommandStub.EditorClose();
        //            vstPluginsInst[0].vstPluginsForm.timer1.Enabled = false;
        //            vstPluginsInst[0].location = vstPluginsInst[0].vstPluginsForm.Location;
        //            vstPluginsInst[0].vstPluginsForm.Close();
        //            if (vstPluginsInst[0].vstPlugins.PluginCommandStub != null) vstPluginsInst[0].vstPlugins.PluginCommandStub.StopProcess();
        //            if (vstPluginsInst[0].vstPlugins.PluginCommandStub != null) vstPluginsInst[0].vstPlugins.PluginCommandStub.MainsChanged(false);
        //            vstPluginsInst[0].vstPlugins.Dispose();
        //        }

        //        vstPluginsInst.RemoveAt(0);
        //    }

        //    while (vstPlugins.Count > 0)
        //    {
        //        if (vstPlugins[0] != null)
        //        {
        //            if (vstPlugins[0].vstPlugins.PluginCommandStub != null) vstPlugins[0].vstPlugins.PluginCommandStub.EditorClose();
        //            vstPlugins[0].vstPluginsForm.timer1.Enabled = false;
        //            vstPlugins[0].location = vstPlugins[0].vstPluginsForm.Location;
        //            vstPlugins[0].vstPluginsForm.Close();
        //            if (vstPlugins[0].vstPlugins.PluginCommandStub != null) vstPlugins[0].vstPlugins.PluginCommandStub.StopProcess();
        //            if (vstPlugins[0].vstPlugins.PluginCommandStub != null) vstPlugins[0].vstPlugins.PluginCommandStub.MainsChanged(false);
        //            vstPlugins[0].vstPlugins.Dispose();
        //        }

        //        vstPlugins.RemoveAt(0);
        //    }
        //}

        //public static List<vstInfo2> getVSTInfos()
        //{
        //    return vstPlugins;
        //}

        //public static vstInfo getVSTInfo(string filename)
        //{
        //    VstPluginContext ctx = OpenPlugin(filename);
        //    if (ctx == null) return null;

        //    vstInfo ret = new vstInfo();
        //    ret.effectName = ctx.PluginCommandStub.GetEffectName();
        //    ret.productName = ctx.PluginCommandStub.GetProductString();
        //    ret.vendorName = ctx.PluginCommandStub.GetVendorString();
        //    ret.programName = ctx.PluginCommandStub.GetProgramName();
        //    ret.fileName = filename;
        //    ret.midiInputChannels = ctx.PluginCommandStub.GetNumberOfMidiInputChannels();
        //    ret.midiOutputChannels = ctx.PluginCommandStub.GetNumberOfMidiOutputChannels();
        //    ctx.PluginCommandStub.Close();

        //    return ret;
        //}

        //public static bool addVSTeffect(string fileName)
        //{
        //    VstPluginContext ctx = OpenPlugin(fileName);
        //    if (ctx == null) return false;

        //    //Stop();

        //    vstInfo2 vi = new vstInfo2();
        //    vi.vstPlugins = ctx;
        //    vi.fileName = fileName;
        //    vi.key = DateTime.Now.Ticks.ToString();
        //    Thread.Sleep(1);

        //    ctx.PluginCommandStub.SetBlockSize(512);
        //    ctx.PluginCommandStub.SetSampleRate(Common.SampleRate);
        //    ctx.PluginCommandStub.MainsChanged(true);
        //    ctx.PluginCommandStub.StartProcess();
        //    vi.effectName = ctx.PluginCommandStub.GetEffectName();
        //    vi.power = true;
        //    ctx.PluginCommandStub.GetParameterProperties(0);


        //    frmVST dlg = new frmVST();
        //    dlg.PluginCommandStub = ctx.PluginCommandStub;
        //    dlg.Show(vi);
        //    vi.vstPluginsForm = dlg;
        //    vi.editor = true;

        //    vstPlugins.Add(vi);

        //    List<vstInfo> lvi = new List<vstInfo>();
        //    foreach (vstInfo2 vi2 in vstPlugins)
        //    {
        //        vstInfo v = new vstInfo();
        //        v.editor = vi.editor;
        //        v.effectName = vi.effectName;
        //        v.fileName = vi.fileName;
        //        v.key = vi.key;
        //        v.location = vi.location;
        //        v.midiInputChannels = vi.midiInputChannels;
        //        v.midiOutputChannels = vi.midiOutputChannels;
        //        v.param = vi.param;
        //        v.power = vi.power;
        //        v.productName = vi.productName;
        //        v.programName = vi.programName;
        //        v.vendorName = vi.vendorName;
        //        lvi.Add(v);
        //    }
        //    setting.vst.VSTInfo = lvi.ToArray();

        //    return true;
        //}

        //public static bool delVSTeffect(string key)
        //{
        //    if (key == "")
        //    {
        //        for (int i = 0; i < vstPlugins.Count; i++)
        //        {
        //            try
        //            {
        //                if (vstPlugins[i].vstPlugins != null)
        //                {
        //                    vstPlugins[i].vstPluginsForm.timer1.Enabled = false;
        //                    vstPlugins[i].location = vstPlugins[i].vstPluginsForm.Location;
        //                    vstPlugins[i].vstPluginsForm.Close();
        //                    vstPlugins[i].vstPlugins.PluginCommandStub.EditorClose();
        //                    vstPlugins[i].vstPlugins.PluginCommandStub.StopProcess();
        //                    vstPlugins[i].vstPlugins.PluginCommandStub.MainsChanged(false);
        //                    vstPlugins[i].vstPlugins.Dispose();
        //                }
        //            }
        //            catch { }
        //        }
        //        vstPlugins.Clear();
        //        setting.vst.VSTInfo = new vstInfo[0];
        //    }
        //    else
        //    {
        //        int ind = -1;
        //        for (int i = 0; i < vstPlugins.Count; i++)
        //        {
        //            //if (vstPlugins[i].fileName == fileName)
        //            if (vstPlugins[i].key == key)
        //            {
        //                ind = i;
        //                break;
        //            }
        //        }

        //        if (ind != -1)
        //        {
        //            try
        //            {
        //                if (vstPlugins[ind].vstPlugins != null)
        //                {
        //                    vstPlugins[ind].vstPluginsForm.timer1.Enabled = false;
        //                    vstPlugins[ind].location = vstPlugins[ind].vstPluginsForm.Location;
        //                    vstPlugins[ind].vstPluginsForm.Close();
        //                    vstPlugins[ind].vstPlugins.PluginCommandStub.EditorClose();
        //                    vstPlugins[ind].vstPlugins.PluginCommandStub.StopProcess();
        //                    vstPlugins[ind].vstPlugins.PluginCommandStub.MainsChanged(false);
        //                    vstPlugins[ind].vstPlugins.Dispose();
        //                }
        //            }
        //            catch { }
        //            vstPlugins.RemoveAt(ind);
        //        }

        //        List<vstInfo> nvst = new List<vstInfo>();
        //        foreach (vstInfo vi in setting.vst.VSTInfo)
        //        {
        //            if (vi.key == key) continue;
        //            nvst.Add(vi);
        //        }
        //        setting.vst.VSTInfo = nvst.ToArray();
        //    }

        //    return true;
        //}



        #region MDSoundインターフェース

        public static int[][] GetFMRegister(int chipID)
        {
            return chipRegister.fmRegisterYM2612[chipID];
        }

        public static int[][] GetYM2612MIDIRegister()
        {
            return mdsMIDI.ReadYM2612Register(0);
        }

        public static int[] GetYM2151Register(int chipID)
        {
            return chipRegister.YM2151FmRegister[chipID];
        }

        public static int[] GetYM2203Register(int chipID)
        {
            return chipRegister.fmRegisterYM2203[chipID];
        }

        public static int[] GetYM2413Register(int chipID)
        {
            return chipRegister.fmRegisterYM2413[chipID];
        }
    
        public static byte[] GetVRC7Register(int chipID)
        {
            return chipRegister.getVRC7Register(chipID);
        }

        public static int[][] GetYM2608Register(int chipID)
        {
            return chipRegister.fmRegisterYM2608[chipID];
        }

        public static int[][] GetYM2610Register(int chipID)
        {
            return chipRegister.fmRegisterYM2610[chipID];
        }

        public static int[] GetYM3526Register(int chipID)
        {
            return chipRegister.fmRegisterYM3526[chipID];
        }

        public static int[] GetY8950Register(int chipID)
        {
            return chipRegister.fmRegisterY8950[chipID];
        }

        public static int[] GetYM3812Register(int chipID)
        {
            return chipRegister.fmRegisterYM3812[chipID];
        }

        public static int[][] GetYMF262Register(int chipID)
        {
            return chipRegister.fmRegisterYMF262[chipID];
        }

        public static int[][] GetYMF278BRegister(int chipID)
        {
            return chipRegister.fmRegisterYMF278B[chipID];
        }

        //public static int[] GetMoonDriverPCMKeyOn()
        //{
        //    if (driver is Driver.MoonDriver.MoonDriver)
        //    {
        //        if (driver != null) return ((Driver.MoonDriver.MoonDriver)driver).GetPCMKeyOn();
        //    }
        //    return null;
        //}

        public static int[] GetPSGRegister(int chipID)
        {
            return chipRegister.SN76489Register[chipID];
        }

        public static int GetPSGRegisterGGPanning(int chipID)
        {
            return chipRegister.SN76489RegisterGGPan[chipID];
        }

        public static int[] GetAY8910Register(int chipID)
        {
            return chipRegister.AY8910PsgRegister[chipID];
        }

        public static Ootake_PSG.huc6280_state GetHuC6280Register(int chipID)
        {
            return mds.ReadHuC6280Status(chipID);
        }

        //public static K051649.k051649_state GetK051649Register(int chipID)
        //{
        //    return mds.ReadK051649Status(chipID);
        //}

        //public static MIDIParam GetMIDIInfos(int chipID)
        //{
        //    return chipRegister.midiParams[chipID];
        //}

        public static scd_pcm.pcm_chip_ GetRf5c164Register(int chipID)
        {
            return mds.ReadRf5c164Register(chipID);
        }

        public static byte[] GetC140Register(int chipID)
        {
            return chipRegister.pcmRegisterC140[chipID];
        }

        public static bool[] GetC140KeyOn(int chipID)
        {
            return chipRegister.pcmKeyOnC140[chipID];
        }

        public static ushort[] GetC352Register(int chipID)
        {
            return chipRegister.pcmRegisterC352[chipID];
        }

        public static ushort[] GetC352KeyOn(int chipID)
        {
            return chipRegister.readC352((byte)chipID);
        }

        public static byte[] GetSEGAPCMRegister(int chipID)
        {
            return chipRegister.pcmRegisterSEGAPCM[chipID];
        }

        public static bool[] GetSEGAPCMKeyOn(int chipID)
        {
            return chipRegister.pcmKeyOnSEGAPCM[chipID];
        }

        public static okim6258.okim6258_state GetOKIM6258Register(int chipID)
        {
            return mds.ReadOKIM6258Status(chipID);
        }

        public static segapcm.segapcm_state GetSegaPCMRegister(int chipID)
        {
            return mds.ReadSegaPCMStatus(chipID);
        }

        public static byte[] GetAPURegister(int chipID)
        {
            byte[] reg = null;

            //nsf向け
            if (chipRegister == null) reg = null;
            else if (chipRegister.nes_apu == null) reg = null;
            else if (chipRegister.nes_apu.chip == null) reg = null;
            else if (chipID == 1) reg = null;
            else reg = chipRegister.nes_apu.chip.reg;

            //vgm向け
            if (reg == null) reg = chipRegister.getNESRegister(chipID);

            return reg;
        }

        public static byte[] GetDMCRegister(int chipID)
        {
            byte[] reg = null;
            try
            {
                //nsf向け
                if (chipRegister == null) reg = null;
                else if (chipRegister.nes_apu == null) reg = null;
                else if (chipRegister.nes_apu.chip == null) reg = null;
                else if (chipID == 1) reg = null;
                else reg = chipRegister.nes_dmc.chip.reg;

                //vgm向け
                //if (reg == null) reg = chipRegister.getNESRegister(chipID, enmModel.VirtualModel);

                return reg;
            }
            catch
            {
                return null;
            }
        }

        public static MDSound.np.np_nes_fds.NES_FDS GetFDSRegister(int chipID)
        {
            MDSound.np.np_nes_fds.NES_FDS reg = null;

            //nsf向け
            if (chipRegister == null) reg = null;
            else if (chipRegister.nes_apu == null) reg = null;
            else if (chipRegister.nes_apu.chip == null) reg = null;
            else if (chipID == 1) reg = null;
            else reg = chipRegister.nes_fds.chip;

            //vgm向け
            if (reg == null) reg = chipRegister.getFDSRegister(chipID, EnmModel.VirtualModel);

            return reg;
        }

        public static byte[] GetMMC5Register(int chipID)
        {
            //nsf向け
            if (chipRegister == null) return null;
            else if (chipRegister.nes_mmc5 == null) return null;
            else if (chipID == 1) return null;

            uint dat = 0;
            for (uint adr = 0x5000; adr < 0x5008; adr++)
            {
                dat = 0;
                chipRegister.nes_mmc5.Read(adr, ref dat);
                mmc5regs[adr & 0x7] = (byte)dat;
            }

            chipRegister.nes_mmc5.Read(0x5010, ref dat);
            mmc5regs[8] = (byte)(chipRegister.nes_mmc5.pcm_mode ? 1 : 0);
            mmc5regs[9] = chipRegister.nes_mmc5.pcm;


            return mmc5regs;
        }

        public static int[] GetFMKeyOn(int chipID)
        {
            return chipRegister.fmKeyOnYM2612[chipID];
        }

        public static int[] GetYM2151KeyOn(int chipID)
        {
            return chipRegister.YM2151FmKeyOn[chipID];
        }

        public static bool GetOKIM6258KeyOn(int chipID)
        {
            return chipRegister.okim6258Keyon[chipID];
        }

        public static void ResetOKIM6258KeyOn(int chipID)
        {
            chipRegister.okim6258Keyon[chipID] = false;
        }

        public static int GetYM2151PMD(int chipID)
        {
            return chipRegister.YM2151FmPMD[chipID];
        }

        public static int GetYM2151AMD(int chipID)
        {
            return chipRegister.YM2151FmAMD[chipID];
        }

        public static int[] GetYM2608KeyOn(int chipID)
        {
            return chipRegister.fmKeyOnYM2608[chipID];
        }

        public static int[] GetYM2610KeyOn(int chipID)
        {
            return chipRegister.fmKeyOnYM2610[chipID];
        }

        public static int[] GetYM2203KeyOn(int chipID)
        {
            return chipRegister.fmKeyOnYM2203[chipID];
        }

        public static ChipKeyInfo getYM2413KeyInfo(int chipID)
        {
            return chipRegister.getYM2413KeyInfo(chipID);
        }

        public static ChipKeyInfo getYM3526KeyInfo(int chipID)
        {
            return chipRegister.getYM3526KeyInfo(chipID);
        }

        public static ChipKeyInfo getY8950KeyInfo(int chipID)
        {
            return chipRegister.getY8950KeyInfo(chipID);
        }

        public static ChipKeyInfo getYM3812KeyInfo(int chipID)
        {
            return chipRegister.getYM3812KeyInfo(chipID);
        }

        public static ChipKeyInfo getVRC7KeyInfo(int chipID)
        {
            return chipRegister.getVRC7KeyInfo(chipID);
        }

        public static int getYMF262FMKeyON(int chipID)
        {
            return chipRegister.getYMF262FMKeyON(chipID);
        }

        public static int getYMF262RyhthmKeyON(int chipID)
        {
            return chipRegister.getYMF262RyhthmKeyON(chipID);
        }

        public static int getYMF278BFMKeyON(int chipID)
        {
            return chipRegister.getYMF278BFMKeyON(chipID);
        }

        public static void resetYMF278BFMKeyON(int chipID)
        {
            chipRegister.resetYMF278BFMKeyON(chipID);
        }

        public static int getYMF278BRyhthmKeyON(int chipID)
        {
            return chipRegister.getYMF278BRyhthmKeyON(chipID);
        }

        public static void resetYMF278BRyhthmKeyON(int chipID)
        {
            chipRegister.resetYMF278BRyhthmKeyON(chipID);
        }

        public static int[] getYMF278BPCMKeyON(int chipID)
        {
            return chipRegister.getYMF278BPCMKeyON(chipID);
        }

        public static void resetYMF278BPCMKeyON(int chipID)
        {
            chipRegister.resetYMF278BPCMKeyON(chipID);
        }


        public static void SetMasterVolume(bool isAbs, int volume)
        {
            MasterVolume
                = setting.balance.MasterVolume
                = Common.Range((isAbs ? 0 : setting.balance.MasterVolume) + volume, -192, 20);
        }

        public static void SetAY8910Volume(bool isAbs, int volume)
        {
            try
            {
                mds.setVolumeAY8910(setting.balance.AY8910Volume
                    = Common.Range((isAbs ? 0 : setting.balance.AY8910Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2151Volume(bool isAbs, int volume)
        {
            try
            {
                int vol
                    = setting.balance.YM2151Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2151Volume) + volume, -192, 20);

                mds.SetVolumeYM2151(vol);
                mds.SetVolumeYM2151mame(vol);
                mds.SetVolumeYM2151x68sound(vol);
            }
            catch { }
        }

        public static void SetYM2203Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2203(setting.balance.YM2203Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2203Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2203FMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2203FM(setting.balance.YM2203FMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2203FMVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2203PSGVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2203PSG(setting.balance.YM2203PSGVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2203PSGVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2413Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2413(setting.balance.YM2413Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2413Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetK053260Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeK053260(setting.balance.K053260Volume
                    = Common.Range((isAbs ? 0 : setting.balance.K053260Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetRF5C68Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeRF5C68(setting.balance.RF5C68Volume
                    = Common.Range((isAbs ? 0 : setting.balance.RF5C68Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM3812Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM3812(setting.balance.YM3812Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM3812Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetY8950Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeY8950(setting.balance.Y8950Volume
                    = Common.Range((isAbs ? 0 : setting.balance.Y8950Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM3526Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM3526(setting.balance.YM3526Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM3526Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2608Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2608(setting.balance.YM2608Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2608Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2608FMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2608FM(setting.balance.YM2608FMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2608FMVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2608PSGVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2608PSG(setting.balance.YM2608PSGVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2608PSGVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2608RhythmVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2608Rhythm(setting.balance.YM2608RhythmVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2608RhythmVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2608AdpcmVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2608Adpcm(setting.balance.YM2608AdpcmVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2608AdpcmVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2610Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2610(setting.balance.YM2610Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2610Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2610FMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2610FM(setting.balance.YM2610FMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2610FMVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2610PSGVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2610PSG(setting.balance.YM2610PSGVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2610PSGVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2610AdpcmAVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2610AdpcmA(setting.balance.YM2610AdpcmAVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2610AdpcmAVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2610AdpcmBVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2610AdpcmB(setting.balance.YM2610AdpcmBVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2610AdpcmBVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYM2612Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYM2612(setting.balance.YM2612Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YM2612Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetSN76489Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeSN76489(setting.balance.SN76489Volume
                    = Common.Range((isAbs ? 0 : setting.balance.SN76489Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetHuC6280Volume(bool isAbs, int volume)
        {
            try
            {
                mds.setVolumeHuC6280(setting.balance.HuC6280Volume
                    = Common.Range((isAbs ? 0 : setting.balance.HuC6280Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetRF5C164Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeRF5C164(setting.balance.RF5C164Volume
                    = Common.Range((isAbs ? 0 : setting.balance.RF5C164Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetPWMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumePWM(setting.balance.PWMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.PWMVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetOKIM6258Volume(bool isAbs, int volume)
        {
            try
            {
                int vol = setting.balance.OKIM6258Volume
                    = Common.Range((isAbs ? 0 : setting.balance.OKIM6258Volume) + volume, -192, 20);

                mds.SetVolumeOKIM6258(vol);
                mds.SetVolumeMpcmX68k(vol);
            }
            catch { }
        }

        public static void SetOKIM6295Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeOKIM6295(setting.balance.OKIM6295Volume
                    = Common.Range((isAbs ? 0 : setting.balance.OKIM6295Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetC140Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeC140(setting.balance.C140Volume
                    = Common.Range((isAbs ? 0 : setting.balance.C140Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetSegaPCMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeSegaPCM(setting.balance.SEGAPCMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.SEGAPCMVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetC352Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeC352(setting.balance.C352Volume
                    = Common.Range((isAbs ? 0 : setting.balance.C352Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetK051649Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeK051649(setting.balance.K051649Volume
                    = Common.Range((isAbs ? 0 : setting.balance.K051649Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetK054539Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeK054539(setting.balance.K054539Volume
                    = Common.Range((isAbs ? 0 : setting.balance.K054539Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetQSoundVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeQSound(setting.balance.QSoundVolume
                    = Common.Range((isAbs ? 0 : setting.balance.QSoundVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetDMGVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeDMG(setting.balance.DMGVolume
                    = Common.Range((isAbs ? 0 : setting.balance.DMGVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetGA20Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeGA20(setting.balance.GA20Volume
                    = Common.Range((isAbs ? 0 : setting.balance.GA20Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYMZ280BVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYMZ280B(setting.balance.YMZ280BVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YMZ280BVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYMF271Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYMF271(setting.balance.YMF271Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YMF271Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYMF262Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYMF262(setting.balance.YMF262Volume
                    = Common.Range((isAbs ? 0 : setting.balance.YMF262Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetYMF278BVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeYMF278B(setting.balance.YMF278BVolume
                    = Common.Range((isAbs ? 0 : setting.balance.YMF278BVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetMultiPCMVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeMultiPCM(setting.balance.MultiPCMVolume
                    = Common.Range((isAbs ? 0 : setting.balance.MultiPCMVolume) + volume, -192, 20));
            }
            catch { }
        }



        public static void SetAPUVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeNES(
                    setting.balance.APUVolume
                    = Common.Range((isAbs ? 0 : setting.balance.APUVolume) + volume, -192, 20)
                    );
            }
            catch { }
        }

        public static void SetDMCVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeDMC(setting.balance.DMCVolume
                    = Common.Range((isAbs ? 0 : setting.balance.DMCVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetFDSVolume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeFDS(setting.balance.FDSVolume
                    = Common.Range((isAbs ? 0 : setting.balance.FDSVolume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetMMC5Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeMMC5(setting.balance.MMC5Volume
                    = Common.Range((isAbs ? 0 : setting.balance.MMC5Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetN160Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeN160(setting.balance.N160Volume
                    = Common.Range((isAbs ? 0 : setting.balance.N160Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetVRC6Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeVRC6(setting.balance.VRC6Volume
                    = Common.Range((isAbs ? 0 : setting.balance.VRC6Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetVRC7Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeVRC7(setting.balance.VRC7Volume
                    = Common.Range((isAbs ? 0 : setting.balance.VRC7Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetFME7Volume(bool isAbs, int volume)
        {
            try
            {
                mds.SetVolumeFME7(setting.balance.FME7Volume
                    = Common.Range((isAbs ? 0 : setting.balance.FME7Volume) + volume, -192, 20));
            }
            catch { }
        }

        public static void SetGimicOPNVolume(bool isAbs, int volume)
        {
            setting.balance.GimicOPNVolume = Common.Range((isAbs ? 0 : setting.balance.GimicOPNVolume) + volume, 0, 127);
        }

        public static void SetGimicOPNAVolume(bool isAbs, int volume)
        {
            setting.balance.GimicOPNAVolume = Common.Range((isAbs ? 0 : setting.balance.GimicOPNAVolume) + volume, 0, 127);
        }


        public static int[] GetFMVolume(int chipID)
        {
            return chipRegister.GetYM2612Volume(chipID);
        }

        public static int[] GetYM2151Volume(int chipID)
        {
            return chipRegister.GetYM2151Volume(chipID);
        }

        public static int[] GetYM2608Volume(int chipID)
        {
            return chipRegister.GetYM2608Volume(chipID);
        }

        public static int[][] GetYM2608RhythmVolume(int chipID)
        {
            return chipRegister.GetYM2608RhythmVolume(chipID);
        }

        public static int[] GetYM2608AdpcmVolume(int chipID)
        {
            return chipRegister.GetYM2608AdpcmVolume(chipID);
        }

        public static int[] GetYM2610Volume(int chipID)
        {
            return chipRegister.GetYM2610Volume(chipID);
        }

        public static int[][] GetYM2610RhythmVolume(int chipID)
        {
            return chipRegister.GetYM2610RhythmVolume(chipID);
        }

        public static int[] GetYM2610AdpcmVolume(int chipID)
        {
            return chipRegister.GetYM2610AdpcmVolume(chipID);
        }

        public static int[] GetYM2203Volume(int chipID)
        {
            return chipRegister.GetYM2203Volume(chipID);
        }

        public static int[] GetFMCh3SlotVolume(int chipID)
        {
            return chipRegister.GetYM2612Ch3SlotVolume(chipID);
        }

        public static int[] GetYM2608Ch3SlotVolume(int chipID)
        {
            return chipRegister.GetYM2608Ch3SlotVolume(chipID);
        }

        public static int[] GetYM2610Ch3SlotVolume(int chipID)
        {
            return chipRegister.GetYM2610Ch3SlotVolume(chipID);
        }

        public static int[] GetYM2203Ch3SlotVolume(int chipID)
        {
            return chipRegister.GetYM2203Ch3SlotVolume(chipID);
        }

        public static int[][] GetPSGVolume(int chipID)
        {
            return chipRegister.GetPSGVolume(chipID);
        }



        public static void setSN76489Mask(int chipID, int ch)
        {
            //mds.setSN76489Mask(chipID,1 << ch);
            chipRegister.SN76489SetMask(chipID, ch, true);
        }

        public static void setRF5C164Mask(int chipID, int ch)
        {
            mds.setRf5c164Mask(chipID, ch);
        }

        public static void setYM2151Mask(int chipID, int ch)
        {
            //mds.setYM2151Mask(ch);
            chipRegister.YM2151SetMask(0, chipID, ch, true);
        }

        public static void setYM2203Mask(int chipID, int ch)
        {
            chipRegister.YM2203SetMask(0, chipID, ch, true);
        }

        public static void setYM2413Mask(int chipID, int ch)
        {
            chipRegister.YM2413SetMask(0, chipID, ch, true);
        }

        public static void setYM2608Mask(int chipID, int ch)
        {
            //mds.setYM2608Mask(ch);
            chipRegister.YM2608SetMask(0, chipID, ch, true);
        }

        public static void setYM2610Mask(int chipID, int ch)
        {
            //mds.setYM2610Mask(ch);
            chipRegister.YM2610SetMask(0,chipID, ch, true);
        }

        public static void setYM2612Mask(int chipID, int ch)
        {
            chipRegister.YM2612SetMask(0, chipID, ch, true);
        }

        public static void setYM3526Mask(int chipID, int ch)
        {
            chipRegister.setMaskYM3526(chipID, ch, true);
        }

        public static void setY8950Mask(int chipID, int ch)
        {
            chipRegister.setMaskY8950(chipID, ch, true);
        }

        public static void setYM3812Mask(int chipID, int ch)
        {
            chipRegister.setMaskYM3812(chipID, ch, true);
        }

        public static void setYMF262Mask(int chipID, int ch)
        {
            chipRegister.setMaskYMF262(chipID, ch, true);
        }

        public static void setYMF278BMask(int chipID, int ch)
        {
            chipRegister.setMaskYMF278B(chipID, ch, true);
        }

        public static void setC140Mask(int chipID, int ch)
        {
            mds.setC140Mask(chipID, 1 << ch);
        }

        public static void setC352Mask(int chipID, int ch)
        {
            chipRegister.setMaskC352(chipID, ch, true);
        }

        public static void setSegaPCMMask(int chipID, int ch)
        {
            mds.setSegaPcmMask(chipID, 1 << ch);
        }

        public static void setAY8910Mask(int chipID, int ch)
        {
            mds.setAY8910Mask(chipID, 1 << ch);
        }

        public static void setHuC6280Mask(int chipID, int ch)
        {
            mds.setHuC6280Mask(chipID, 1 << ch);
        }

        public static void setOKIM6258Mask(int chipID)
        {
            chipRegister.setMaskOKIM6258(chipID, true);
        }

        public static void setNESMask(int chipID, int ch)
        {
            chipRegister.setNESMask(chipID, ch);
        }

        public static void setDMCMask(int chipID, int ch)
        {
            chipRegister.setNESMask(chipID, ch + 2);
        }

        public static void setFDSMask(int chipID)
        {
            chipRegister.setFDSMask(chipID);
        }

        public static void setMMC5Mask(int chipID, int ch)
        {
            chipRegister.setMMC5Mask(chipID, ch);
        }

        public static void setVRC7Mask(int chipID, int ch)
        {
            chipRegister.setVRC7Mask(chipID, ch);
        }

        public static void setK051649Mask(int chipID, int ch)
        {
            chipRegister.setK051649Mask(chipID, ch);
        }


        public static void resetSN76489Mask(int chipID, int ch)
        {
            try
            {
                //mds.resetSN76489Mask(chipID, 1 << ch);
                chipRegister.SN76489SetMask(chipID, ch, false);
            }
            catch { }
        }

        public static void resetYM2608Mask(int chipID, int ch)
        {
            try
            {
                //mds.resetYM2608Mask(ch);
                chipRegister.YM2608SetMask(0, chipID, ch, false, Stopped);
            }
            catch { }
        }

        public static void resetYM2612Mask(int chipID, int ch)
        {
            try
            {
                //mds.resetYM2612Mask(chipID, 1 << ch);
                chipRegister.YM2612SetMask(0, chipID, ch, false);
            }
            catch { }
        }

        public static void resetOKIM6258Mask(int chipID)
        {
            chipRegister.setMaskOKIM6258(chipID, false);
        }

        public static void resetYM2203Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.YM2203SetMask(0, chipID, ch, false, Stopped);
            }
            catch { }
        }

        public static void resetYM2413Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.YM2413SetMask(0, chipID, ch, false, Stopped);
            }
            catch { }
        }

        public static void resetRF5C164Mask(int chipID, int ch)
        {
            try
            {
                mds.resetRf5c164Mask(chipID, ch);
            }
            catch { }
        }

        public static void resetYM2151Mask(int chipID, int ch)
        {
            try
            {
                //mds.resetYM2151Mask(ch);
                chipRegister.YM2151SetMask(0, chipID, ch, false);//, Stopped);
            }
            catch { }
        }

        public static void resetYM2610Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.YM2610SetMask(0, chipID, ch, false);
            }
            catch { }
        }

        public static void resetYM3526Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskYM3526(chipID, ch, false);
            }
            catch { }
        }

        public static void resetY8950Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskY8950(chipID, ch, false);
            }
            catch { }
        }

        public static void resetYM3812Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskYM3812(chipID, ch, false);
            }
            catch { }
        }

        public static void resetYMF262Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskYMF262(chipID, ch, false);
            }
            catch { }
        }

        public static void resetYMF278BMask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskYMF278B(chipID, ch, false);
            }
            catch { }
        }

        public static void resetC140Mask(int chipID, int ch)
        {
            mds.resetC140Mask(chipID, 1 << ch);
        }

        public static void resetC352Mask(int chipID, int ch)
        {
            try
            {
                chipRegister.setMaskC352(chipID, ch, false);
            }
            catch { }
        }

        public static void resetSegaPCMMask(int chipID, int ch)
        {
            mds.resetSegaPcmMask(chipID, 1 << ch);
        }

        public static void resetAY8910Mask(int chipID, int ch)
        {
            mds.resetAY8910Mask(chipID, 1 << ch);
        }

        public static void resetHuC6280Mask(int chipID, int ch)
        {
            mds.resetHuC6280Mask(chipID, 1 << ch);
        }

        public static void resetNESMask(int chipID, int ch)
        {
            chipRegister.resetNESMask(chipID, ch);
        }

        public static void resetDMCMask(int chipID, int ch)
        {
            chipRegister.resetNESMask(chipID, ch + 2);
        }

        public static void resetFDSMask(int chipID)
        {
            chipRegister.resetFDSMask(chipID);
        }

        public static void resetMMC5Mask(int chipID, int ch)
        {
            chipRegister.resetMMC5Mask(chipID, ch);
        }

        public static void resetVRC7Mask(int chipID, int ch)
        {
            chipRegister.resetVRC7Mask(chipID, ch);
        }

        public static void resetK051649Mask(int chipID, int ch)
        {
            chipRegister.resetK051649Mask(chipID, ch);
        }

        #endregion



    }


}
