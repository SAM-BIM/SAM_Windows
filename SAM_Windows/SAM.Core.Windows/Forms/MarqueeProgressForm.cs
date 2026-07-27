// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace SAM.Core.Windows.Forms
{
    public partial class MarqueeProgressForm : Form
    {
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();
        private List<Tuple<Action, string>> tuples;

        /// <summary>Runs from construction; a marquee has no step to measure, so this is the whole stage.</summary>
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private string description;

        /// <summary>Whether a <see cref="Note"/> is currently set, and so whether the form is grown for it.</summary>
        private bool noted;

        /// <summary>Designer height, used while no <see cref="Note"/> is set (the default).</summary>
        private const int CollapsedClientHeight = 94;

        /// <summary>Height needed to fit the two-line note above the bar.</summary>
        private const int NotedClientHeight = 114;

        /// <summary>Progress bar top in the collapsed layout (the original designer position).</summary>
        private const int ProgressBarTopCollapsed = 46;

        /// <summary>Progress bar top once the note is shown.</summary>
        private const int ProgressBarTopNoted = 66;

        public MarqueeProgressForm(string name)
        {
            InitializeComponent();

            Text = name;

            ProgressBar_Main.Style = ProgressBarStyle.Marquee;
            ProgressBar_Main.MarqueeAnimationSpeed = 30;
        }

        /// <summary>
        /// Main line shown above the bar. Setting it also starts the elapsed readout appended to it, so a
        /// caller that leaves it null gets the original bar-only form with no timer running.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
                Timer_Elapsed.Enabled = !string.IsNullOrEmpty(description);
                UpdateDescription();
            }
        }

        /// <summary>
        /// Secondary grey text under <see cref="Description"/> — use it to say that the stage cannot be
        /// cancelled. Setting it grows the form to fit two wrapped lines; left empty (the default) the form
        /// keeps its original height, so callers that do not set it are unchanged.
        /// </summary>
        public string Note
        {
            get
            {
                return Label_Note.Text;
            }
            set
            {
                string text = value ?? string.Empty;
                bool value_Noted = text.Length != 0;

                Label_Note.Text = text;

                // Tracked in a field rather than read back from Label_Note.Visible: on a form that has not
                // been shown yet that getter reports effective visibility (false), so a set-then-clear round
                // trip would early-return here and leave the form expanded.
                if (noted == value_Noted)
                {
                    return;
                }

                noted = value_Noted;
                Label_Note.Visible = value_Noted;
                ProgressBar_Main.Top = noted ? ProgressBarTopNoted : ProgressBarTopCollapsed;
                ClientSize = new System.Drawing.Size(ClientSize.Width, noted ? NotedClientHeight : CollapsedClientHeight);
            }
        }

        /// <summary>Total time this form has been up, i.e. how long the uninterruptible call has run.</summary>
        public TimeSpan Elapsed
        {
            get
            {
                return stopwatch.Elapsed;
            }
        }

        private void Timer_Elapsed_Tick(object sender, EventArgs e)
        {
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            // No description means no caller opted in, so leave the label blank rather than showing a bare
            // elapsed time on a form that never had any text.
            Label_Description.Text = string.IsNullOrEmpty(description)
                ? string.Empty
                : description + " | total " + Query.Duration(stopwatch.Elapsed);
        }

        public MarqueeProgressForm(string name, Action action)
        {
            InitializeComponent();
            
            if(action != null)
            {
                tuples = new List<Tuple<Action, string>>() { new Tuple<Action, string>(action, name) };
            }

            if(tuples != null && tuples.Count != 0)
            {
                Text = tuples[0].Item2;
            }

            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            backgroundWorker.RunWorkerAsync();
        }

        public MarqueeProgressForm(IEnumerable<Tuple<Action, string>> actions)
        {
            InitializeComponent();

            tuples = actions == null ? null : new List<Tuple<Action, string>>(actions);


            if (tuples != null && tuples.Count != 0)
            {
                Text = tuples[0].Item2;
            }

            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            backgroundWorker.RunWorkerAsync();
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBar_Main.Style = ProgressBarStyle.Continuous;
            ProgressBar_Main.MarqueeAnimationSpeed = 0;

            Close();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ProgressBar_Main.Style = ProgressBarStyle.Marquee;
            ProgressBar_Main.MarqueeAnimationSpeed = 30;

            if(tuples != null)
            {
                foreach(Tuple<Action, string> tuple in tuples)
                {
                    Text = tuple.Item2;
                    if(tuple.Item1 != null)
                    {
                        tuple.Item1.Invoke();
                    }
                }

                
            }
        }

        public static void Show(string name, Action action)
        {
            using (MarqueeProgressForm marqueeProgressForm = new MarqueeProgressForm(name, action))
            {
                if (marqueeProgressForm.ShowDialog() == DialogResult.OK)
                {

                }
            }
        }

        public static void Show(IEnumerable<Tuple<Action, string>> actions)
        {
            using (MarqueeProgressForm marqueeProgressForm = new MarqueeProgressForm(actions))
            {
                if (marqueeProgressForm.ShowDialog() == DialogResult.OK)
                {

                }
            }
        }

        public static void Show(string name, Action action, IWin32Window owner)
        {
            using (MarqueeProgressForm marqueeProgressForm = new MarqueeProgressForm(name, action))
            {
                if (marqueeProgressForm.ShowDialog(owner) == DialogResult.OK)
                {

                }
            }
        }

        public static void Show(IEnumerable<Tuple<Action, string>> actions, IWin32Window owner)
        {
            using (MarqueeProgressForm marqueeProgressForm = new MarqueeProgressForm(actions))
            {
                if (marqueeProgressForm.ShowDialog(owner) == DialogResult.OK)
                {

                }
            }
        }
    }
}
