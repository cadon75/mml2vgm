﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Const
    {

        public const int INSTRUMENT_SIZE = 39 + 8;
        public const int OPL_INSTRUMENT_SIZE = 11 + 11 + 2 + 1;
        public const int INSTRUMENT_OPERATOR_SIZE = 9;
        public const int INSTRUMENT_M_OPERATOR_SIZE = 11;
        public const int WF_INSTRUMENT_SIZE = 33;

        public const string TITLENAME = "TITLENAME";
        public const string TITLENAMEJ = "TITLENAMEJ";
        public const string GAMENAME = "GAMENAME";
        public const string GAMENAMEJ = "GAMENAMEJ";
        public const string SYSTEMNAME = "SYSTEMNAME";
        public const string SYSTEMNAMEJ = "SYSTEMNAMEJ";
        public const string COMPOSER = "COMPOSER";
        public const string COMPOSERJ = "COMPOSERJ";
        public const string RELEASEDATE = "RELEASEDATE";
        public const string CONVERTED = "CONVERTED";
        public const string NOTES = "NOTES";
        public const string PARTNAME = "PART";
        public const string CLOCKCOUNT = "CLOCKCOUNT";
        public const string FMF_NUM = "FMF-NUM";
        public const string PSGF_NUM = "PSGF-NUM";
        public const string FORCEDMONOPARTYM2612 = "FORCEDMONOPARTYM2612";
        public const string VERSION = "VERSION";
        public const string PRIMARY = "PRIMARY";
        public const string SECONDARY = "SECONDARY";
        public const string FORMAT = "FORMAT";
        public const string XGMBASEFRAME = "XGMBASEFRAME";
        public const string OCTAVEREV = "OCTAVE-REV";

        public const string NOTE = "c_d_ef_g_a_b";

        readonly public static string[] IDName = new string[] { Const.PRIMARY, Const.SECONDARY };

        /*
C   ド	    261.62
C#	ド#	    277.18 1.05947557526183
D   レ	    293.66 1.122467701246082
D   レ#	    311.12 1.189205718217262
E   ミ	    329.62 1.259918966439875
F   ファ	349.22 1.334836786178427
F#	ファ#	369.99 1.414226741074841
G   ソ	    391.99 1.498318171393624
G#	ソ#	    415.30 1.587416864154117
A   ラ	    440.00 1.681828606375659
A#	ラ#	    466.16 1.781820961700176
B   シ	    493.88 1.887776163901842
*/
        readonly public static float[] pcmMTbl = new float[]
        {
            1.0f
            ,1.05947557526183f
            ,1.122467701246082f
            ,1.189205718217262f
            ,1.259918966439875f
            ,1.334836786178427f
            ,1.414226741074841f
            ,1.498318171393624f
            ,1.587416864154117f
            ,1.681828606375659f
            ,1.781820961700176f
            ,1.887776163901842f
        };

        //header
        readonly public static byte[] hDat = new byte[] {
                //'Vgm '
                0x56,0x67,0x6d,0x20,
                //Eof offset(see below)
                0x00,0x00,0x00,0x00,
                //Version number(v1.51(0x0000151))
                0x61,0x01,0x00,0x00,
                //SN76489(0x369e99)
                0x99,0x9e,0x36,0x00,
                //YM2413 clock(3579545 0x369e99)
                0x00,0x00,0x00,0x00,//0x99,0x9e,0x36,0x00,
                //GD3 offset(no use)
                0x00,0x00,0x00,0x00,
                //Total # samples(see below)
                0x00,0x00,0x00,0x00,
                //Loop offset(no use)
                0x00,0x00,0x00,0x00,
                //Loop # samples(no use)
                0x00,0x00,0x00,0x00,
                //Rate(NTSC 60Hz)
                0x3c,0x00,0x00,0x00,
                //SN76489 feedback(0x09 Mega Drive)
                0x09,0x00,
                //SN76489 shift register width(0x10 Mega Drive)
                0x10,
                //SN76489 Flags(0x00 version 1.51 and later)
                0x00,
                //0x2c YM2612 clock(0x750ab5)
                0xb5,0x0a,0x75,0x00,
                //0x30 YM2151 clock(3579545 0x369e99)
                0x99,0x9e,0x36,0x00,
                //0x34 VGM data offset(1.50 only)
                0x0c+16*7,0x00,0x00,0x00,
                //0x38 Sega PCM clock(no use)
                0x6b,0x72,0x3d,0x00,
                //0x3c Sega PCM interface register(no use)
                0x0d,0x00,0xf8,0x00
                //0x40 RF5C68 clock(no use)
                ,0x00,0x00,0x00,0x00,
                //0x44 YM2203 clock(3993600 0x3cf000)
                0x00,0xf0,0x3c,0x00,
                //0x48 YM2608 clock(7987200 0x79e000)
                0x00,0xe0,0x79,0x00,
                //0x4c YM2610/B clock(0x7a1200)
                0x00,0x12,0x7a,0x00,
                //0x50               0x54                 0x58                 0x5c
                0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
                //0x60
                0x00,0x00,0x00,0x00,
                //0x64
                0x00,0x00,0x00,0x00,
                //0x68
                0x00,0x00,0x00,0x00,
                //0x6c RF5C164 clock(0xbebc20)
                0x20,0xbc,0xbe,0x00,
                //0x70               0x74                 0x78                 0x7c
                0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
                //0x80               0x84                 0x88                 0x8c
                0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
                //0x90               0x94                 0x98                 0x9c
                0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00,
                //0xa0               0xa4 HuC6280         0xa8                 0xac
                0x00,0x00,0x00,0x00, 0x99,0x9e,0x36,0x00, 0x00,0x00,0x00,0x00, 0x00,0x00,0x00,0x00
            };

        readonly public static byte[] xhDat = new byte[] {
                //$0000 'XGM '
                0x58,0x47,0x4d,0x20,
                //$0004 Sample id table(63samples)
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00,
                0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 0xff,0xff,0x00,0x00, 
                //$0100 Sample data bloc size / 256
                0x00,0x00,
                //$0102 Version information (0x01 currently)
                0x01,
                //$0103 bit #0: NTSC / PAL information
                //      bit #1: GD3 tags
                //      bit #2: Multi track file
                0x02
                //以下、可変長
        };


    }

}
