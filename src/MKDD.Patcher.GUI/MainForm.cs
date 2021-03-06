﻿using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MKDD.Patcher.GUI
{
    public partial class MainForm : Form
    {
        private const string MSG_ABOUT = @"
MKDD Patcher GUI made by TGE.
Special thanks to:
- Lunaboy (LunaboyRarcTools)
- arookas (mareep)";
        private Logger mLogger;
        private Logger mPatcherLogger;
        private GuiConfig mConfiguration;
        private MergeOrder mMergeOrder;
        private ModDb mModDb;
        private BindingList<ModListVm> mModVms;
        private bool mIsPatching;

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(Logger logger, GuiConfig configuration)
            : this()
        {
            var asmName = Assembly.GetExecutingAssembly().GetName();
            Text = $"{asmName.Name} {asmName.Version.Major}.{asmName.Version.Minor}.{asmName.Version.Revision} by TGE";

            mLogger = logger;
            mPatcherLogger = new LoggerConfiguration()
                .WriteTo.Logger(mLogger)
                .WriteTo.Sink(new DelegateSink(x =>
                {
                    if ( x.Level >= LogEventLevel.Information )
                        Task.Run( () => InvokeOnUIThread( () => LogToRtb( x ) ) );
                }))
                .CreateLogger();

            mConfiguration = configuration;
            mModVms = new BindingList<ModListVm>();

            clmEnabled.DataPropertyName = nameof( ModListVm.Enabled );
            clmTitle.DataPropertyName = nameof( ModListVm.Title );
            clmVersion.DataPropertyName = nameof( ModListVm.Version );
            clmAuthors.DataPropertyName = nameof( ModListVm.Authors );
            clmDescription.DataPropertyName = nameof( ModListVm.Description );

            Initialize();
        }

        private void LogToRtb( LogEvent logEvent )
        {
            var message = logEvent.RenderMessage();
            switch ( logEvent.Level )
            {
                case LogEventLevel.Verbose:
                    rtbLog.ForeColor = Color.Black;
                    break;
                case LogEventLevel.Debug:
                    rtbLog.ForeColor = Color.Green;
                    break;
                case LogEventLevel.Information:
                    rtbLog.ForeColor = Color.Black;
                    break;
                case LogEventLevel.Warning:
                    rtbLog.ForeColor = Color.Yellow;
                    break;
                case LogEventLevel.Error:
                    rtbLog.ForeColor = Color.Red;
                    break;
                case LogEventLevel.Fatal:
                    rtbLog.ForeColor = Color.DarkRed;
                    break;
            }
            rtbLog.AppendText( message + "\n" );
        }

        private void Initialize()
        {
            mMergeOrder = MergeOrder.BottomToTop;
            mModDb = new ModDb( mLogger, mConfiguration.Patcher, mConfiguration.Patcher.ModsDir );
            PopulateGrid();
        }


        private void PopulateGrid()
        {
            mModVms.Clear();
            dgvMods.DataSource = mModVms;

            // Iterate over mods 
            var indexedMods = new List<(int Index, ModInfo DbModInfo, GuiModConfig GuiModInfo)>();
            foreach ( var modInfo in mModDb.Mods )
            {
                var index = mConfiguration.Mods.FindIndex(x => x.Title == modInfo.Title);
                if ( index == -1 )
                {
                    var guiModInfo = new GuiModConfig() { Enabled = true, Title = modInfo.Title };
                    mConfiguration.Mods.Add( guiModInfo );
                    indexedMods.Add( (int.MaxValue, modInfo, guiModInfo) );
                }
                else
                {
                    var guiModInfo = mConfiguration.Mods[index];
                    indexedMods.Add( (index, modInfo, guiModInfo) );
                }
            }

            foreach ( var item in indexedMods.OrderBy( x => x.Index ) )
                mModVms.Add( new ModListVm( item.DbModInfo, item.GuiModInfo ) );

            SaveConfiguration();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var enabledMods = GetEnabledMods();
            btnSave.Enabled = false;
            var patchTask = Task.Run(() =>
            {
                mIsPatching = true;
                var patcher = new Patcher(mPatcherLogger, mConfiguration.Patcher, mModDb);
                patcher.Patch(mMergeOrder, enabledMods);
            }).ContinueWith(task =>
            {
                MessageBox.Show("Patching done!");
                mIsPatching = false;
                InvokeOnUIThread(() => btnSave.Enabled = true );
            });
        }

        private void InvokeOnUIThread(Action action)
        {
            Invoke( action );
        }

        private int GetModOrder(string title)
        {
            for ( int i = 0; i < mModVms.Count; i++ )
            {
                if ( mModVms[i].Title == title )
                    return i;
            }

            return -1;
        }

        private void SaveConfiguration()
        {
            var orderedMods = mConfiguration.Mods.OrderBy( x => GetModOrder( x.Title ) )
                .ToList();

            mConfiguration.Mods.Clear();
            mConfiguration.Mods.AddRange( orderedMods );
            mConfiguration.Save( GuiConfig.FILE_PATH );
        }

        private List<string> GetEnabledMods()
        {
            SaveConfiguration();

            var enabledMods = new List<string>();
            for ( int i = 0; i < mConfiguration.Mods.Count; i++ )
            {
                if ( mConfiguration.Mods[i].Enabled )
                    enabledMods.Add( mConfiguration.Mods[i].Title );
            }

            return enabledMods;
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            var text = MSG_ABOUT;

            MessageBox.Show(text, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            MoveSelectedRows(-1);
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            MoveSelectedRows(1);
        }

        private void MoveSelectedRows(int offset)
        {
            var changedSelRowIndices = new List<int>();
            foreach (DataGridViewRow row in dgvMods.SelectedRows)
            {
                var index = row.Index;
                var newIndex = index + offset;
                if (newIndex >= 0 && newIndex < dgvMods.Rows.Count)
                {
                    var item = mModVms[index];
                    mModVms.RemoveAt( index );
                    mModVms.Insert( newIndex, item );
                    changedSelRowIndices.Add( newIndex );
                }
            }

            dgvMods.ClearSelection();
            foreach (var item in changedSelRowIndices)
                dgvMods.Rows[item].Cells[0].Selected = true;
        }

        private void tsmiSettings_Click(object sender, EventArgs e)
        {
            using ( var dialog = new ConfigurationForm( mConfiguration ) )
            {
                if ( dialog.ShowDialog() == DialogResult.OK )
                {
                    SaveConfiguration();
                    Initialize();
                }
            }
        }

        private void rtbLog_TextChanged( object sender, EventArgs e )
        {
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }

        private void btnAdd_Click( object sender, EventArgs e )
        {
            using ( var dialog = new EditModForm( mConfiguration, null ) )
            {
                if ( dialog.ShowDialog() != DialogResult.OK )
                    return;

                var modInfo = dialog.ModInfo;
                Directory.CreateDirectory( modInfo.RootDir );
                Directory.CreateDirectory( modInfo.FilesDir );
                modInfo.Save( Path.Combine( modInfo.RootDir, ModInfo.FILENAME ) );
                Initialize();
                Process.Start( modInfo.FilesDir );
            }
        }

        private void btnDel_Click( object sender, EventArgs e )
        {
            if ( MessageBox.Show( "This will permanently delete the files from your system.\nAre you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) != DialogResult.Yes )
                return;

            foreach ( DataGridViewRow row in dgvMods.SelectedRows )
            {
                var item = mModVms[row.Index];

                try
                {
                    Directory.Delete( item.ModInfo.RootDir, true );
                }
                catch ( IOException )
                {
                    mLogger.Error( $"Can not delete directory: {item.ModInfo.RootDir}. Might still be in use." );
                }

            }

            Initialize();
        }

        private void dgvMods_CellMouseDoubleClick( object sender, DataGridViewCellMouseEventArgs e )
        {
            using ( var dialog = new EditModForm( mConfiguration, mModVms[e.RowIndex].ModInfo ) )
            {
                if ( dialog.ShowDialog() != DialogResult.OK ) return;
            }
        }
    }
}
