﻿using NAudio.Wave;
using System;
using System.Threading;

namespace mml2vgmIDE
{
    public class NullOut : IWavePlayer, IDisposable
    {
        private bool isNoWaitMode;

        public NullOut(bool isNoWaitMode)
        {
            this.isNoWaitMode = isNoWaitMode;
        }

        public PlaybackState PlaybackState
        {
            get
            {
                return pbState;
            }
        }

        public float Volume
        {
            get
            {
                return 0f;
            }
            set
            {
                ;
            }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped = null;

        public void Dispose()
        {
            if (PlaybackStopped != null) PlaybackStopped = null;
        }

        public void Init(IWaveProvider waveProvider)
        {
            //初期化
            wP = waveProvider;
        }

        public void Pause()
        {
        }

        public void Play()
        {
            //レンダリング開始
            RequestPlay();
        }

        public void Stop()
        {
            //レンダリング停止
            reqStop = true;
        }



        private Thread trdMain;
        private IWaveProvider wP;
        private byte[] buf = new byte[4000];
        private PlaybackState pbState = PlaybackState.Stopped;
        private bool reqStop = false;

        private void RequestPlay()
        {
            if (trdMain != null)
            {
                return;
            }

            reqStop = false;
            trdMain = new Thread(new ThreadStart(trdFunction));
            trdMain.Priority = ThreadPriority.Highest;
            trdMain.IsBackground = true;
            trdMain.Name = "trdNullOutFunction";
            trdMain.Start();
        }

        private void trdFunction()
        {
            pbState = PlaybackState.Playing;

            while (!reqStop)
            {
                wP.Read(buf, 0, 4000);
            }

            pbState = PlaybackState.Stopped;
        }
    }
}
