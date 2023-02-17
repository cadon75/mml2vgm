﻿using mml2vgmIDEx64.Properties;

namespace mml2vgmIDE
{
    partial class FrmMixer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMixer));
            this.pbScreen = new System.Windows.Forms.PictureBox();
            this.ctxtMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiLoadDriverBalance = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLoadSongBalance = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiSaveDriverBalance = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSaveSongBalance = new System.Windows.Forms.ToolStripMenuItem();
            this.ウィンドウサイズ変更ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiX1 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiX2 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiX3 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiX4 = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen)).BeginInit();
            this.ctxtMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbScreen
            // 
            this.pbScreen.ContextMenuStrip = this.ctxtMenu;
            this.pbScreen.Image = Resources.planeMixer;
            this.pbScreen.Location = new System.Drawing.Point(0, 0);
            this.pbScreen.Name = "pbScreen";
            this.pbScreen.Size = new System.Drawing.Size(320, 288);
            this.pbScreen.TabIndex = 1;
            this.pbScreen.TabStop = false;
            this.pbScreen.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PbScreen_MouseClick);
            this.pbScreen.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FrmMixer_MouseDown);
            this.pbScreen.MouseEnter += new System.EventHandler(this.PbScreen_MouseEnter);
            this.pbScreen.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FrmMixer_MouseMove);
            // 
            // ctxtMenu
            // 
            this.ctxtMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiLoadDriverBalance,
            this.tsmiLoadSongBalance,
            this.toolStripSeparator1,
            this.tsmiSaveDriverBalance,
            this.tsmiSaveSongBalance,
            this.ウィンドウサイズ変更ToolStripMenuItem});
            this.ctxtMenu.Name = "ctxtMenu";
            this.ctxtMenu.Size = new System.Drawing.Size(224, 142);
            // 
            // tsmiLoadDriverBalance
            // 
            this.tsmiLoadDriverBalance.Enabled = false;
            this.tsmiLoadDriverBalance.Name = "tsmiLoadDriverBalance";
            this.tsmiLoadDriverBalance.Size = new System.Drawing.Size(223, 22);
            this.tsmiLoadDriverBalance.Text = "読込　ドライバーミキサーバランス";
            this.tsmiLoadDriverBalance.Visible = false;
            this.tsmiLoadDriverBalance.Click += new System.EventHandler(this.TsmiLoadDriverBalance_Click);
            // 
            // tsmiLoadSongBalance
            // 
            this.tsmiLoadSongBalance.Enabled = false;
            this.tsmiLoadSongBalance.Name = "tsmiLoadSongBalance";
            this.tsmiLoadSongBalance.Size = new System.Drawing.Size(223, 22);
            this.tsmiLoadSongBalance.Text = "読込　ソングミキサーバランス";
            this.tsmiLoadSongBalance.Visible = false;
            this.tsmiLoadSongBalance.Click += new System.EventHandler(this.TsmiLoadSongBalance_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(220, 6);
            this.toolStripSeparator1.Visible = false;
            // 
            // tsmiSaveDriverBalance
            // 
            this.tsmiSaveDriverBalance.Name = "tsmiSaveDriverBalance";
            this.tsmiSaveDriverBalance.Size = new System.Drawing.Size(223, 22);
            this.tsmiSaveDriverBalance.Text = "保存　ドライバーミキサーバランス";
            this.tsmiSaveDriverBalance.Visible = false;
            this.tsmiSaveDriverBalance.Click += new System.EventHandler(this.TsmiSaveDriverBalance_Click);
            // 
            // tsmiSaveSongBalance
            // 
            this.tsmiSaveSongBalance.Name = "tsmiSaveSongBalance";
            this.tsmiSaveSongBalance.Size = new System.Drawing.Size(223, 22);
            this.tsmiSaveSongBalance.Text = "保存　ソングミキサーバランス";
            this.tsmiSaveSongBalance.Visible = false;
            this.tsmiSaveSongBalance.Click += new System.EventHandler(this.TsmiSaveSongBalance_Click);
            // 
            // ウィンドウサイズ変更ToolStripMenuItem
            // 
            this.ウィンドウサイズ変更ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiX1,
            this.tsmiX2,
            this.tsmiX3,
            this.tsmiX4});
            this.ウィンドウサイズ変更ToolStripMenuItem.Name = "ウィンドウサイズ変更ToolStripMenuItem";
            this.ウィンドウサイズ変更ToolStripMenuItem.Size = new System.Drawing.Size(223, 22);
            this.ウィンドウサイズ変更ToolStripMenuItem.Text = "ウィンドウサイズ変更";
            // 
            // tsmiX1
            // 
            this.tsmiX1.Name = "tsmiX1";
            this.tsmiX1.Size = new System.Drawing.Size(180, 22);
            this.tsmiX1.Text = "x1";
            this.tsmiX1.Click += new System.EventHandler(this.tsmiX_Click);
            // 
            // tsmiX2
            // 
            this.tsmiX2.Name = "tsmiX2";
            this.tsmiX2.Size = new System.Drawing.Size(180, 22);
            this.tsmiX2.Text = "x2";
            this.tsmiX2.Click += new System.EventHandler(this.tsmiX_Click);
            // 
            // tsmiX3
            // 
            this.tsmiX3.Name = "tsmiX3";
            this.tsmiX3.Size = new System.Drawing.Size(180, 22);
            this.tsmiX3.Text = "x3";
            this.tsmiX3.Click += new System.EventHandler(this.tsmiX_Click);
            // 
            // tsmiX4
            // 
            this.tsmiX4.Name = "tsmiX4";
            this.tsmiX4.Size = new System.Drawing.Size(180, 22);
            this.tsmiX4.Text = "x4";
            this.tsmiX4.Click += new System.EventHandler(this.tsmiX_Click);
            // 
            // FrmMixer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 288);
            this.Controls.Add(this.pbScreen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FrmMixer";
            this.Text = "Mixer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmMixer_FormClosed);
            this.Load += new System.EventHandler(this.FrmMixer_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmMixer_KeyDown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FrmMixer_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FrmMixer_MouseMove);
            ((System.ComponentModel.ISupportInitialize)(this.pbScreen)).EndInit();
            this.ctxtMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PictureBox pbScreen;
        private System.Windows.Forms.ContextMenuStrip ctxtMenu;
        private System.Windows.Forms.ToolStripMenuItem tsmiLoadDriverBalance;
        private System.Windows.Forms.ToolStripMenuItem tsmiLoadSongBalance;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveDriverBalance;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveSongBalance;
        private System.Windows.Forms.ToolStripMenuItem ウィンドウサイズ変更ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiX1;
        private System.Windows.Forms.ToolStripMenuItem tsmiX2;
        private System.Windows.Forms.ToolStripMenuItem tsmiX3;
        private System.Windows.Forms.ToolStripMenuItem tsmiX4;
    }
}