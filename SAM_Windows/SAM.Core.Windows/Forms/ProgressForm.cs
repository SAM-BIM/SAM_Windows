// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SAM.Core.Windows.Forms
{
    public partial class ProgressForm : Form
    {
        private string caption;
        private int maxLength = 80;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Requested cancellability. Tracked separately from <c>Button_Cancel.Visible</c>, whose getter
        /// reports *effective* visibility: on a form that has not been shown yet it reads false even after
        /// being set true, so using it as the backing store would make a true-then-false round trip leave the
        /// button enabled and the form expanded.
        /// </summary>
        private bool cancellable;

        /// <summary>Designer height, used when the Cancel button is hidden (the default).</summary>
        private const int CollapsedClientHeight = 98;

        /// <summary>Height needed to show the Cancel button beneath the progress bar.</summary>
        private const int CancellableClientHeight = 135;

        /// <summary>
        /// Raised on the UI thread when the user clicks Cancel. Because <see cref="Update"/> pumps the
        /// message queue (<see cref="Application.DoEvents"/>) on every step, this fires between steps even
        /// while a synchronous caller is blocking the UI thread — no background thread is involved.
        /// </summary>
        public event System.EventHandler CancelRequested;

        public ProgressForm()
        {
            InitializeComponent();
        }

        public ProgressForm(string name, int max)
            : this(name)
        {
            ProgressBar_Main.Maximum = max;
        }

        public ProgressForm(string name)
        {
            InitializeComponent();
            Text = name;

            ProgressBar_Main.Minimum = 0;
            //ProgressBar_Main.Maximum = max;
            ProgressBar_Main.Step = 1;
            ProgressBar_Main.Value = 0;

            Show(new WindowHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle));

            Application.DoEvents();
        }

        /// <summary>
        /// Opt-in: shows the Cancel button and grows the form to fit it. Off by default, and the form keeps
        /// its original height, so existing callers of this shared form are visually unchanged. Callers that
        /// set this should honour <see cref="CancellationRequested"/> (or subscribe to
        /// <see cref="CancelRequested"/>) between steps.
        /// </summary>
        public bool Cancellable
        {
            get
            {
                return cancellable;
            }
            set
            {
                if (cancellable == value)
                {
                    return;
                }

                cancellable = value;
                Button_Cancel.Visible = value;
                ClientSize = new System.Drawing.Size(ClientSize.Width, value ? CancellableClientHeight : CollapsedClientHeight);
            }
        }

        /// <summary>True once the user has clicked Cancel.</summary>
        public bool CancellationRequested
        {
            get;
            private set;
        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            CancellationRequested = true;
            Button_Cancel.Enabled = false;
            Label_Description.Text = "Cancelling... (finishing current step)";
            Refresh();
            CancelRequested?.Invoke(this, System.EventArgs.Empty);
            Application.DoEvents();
        }

        public string Caption
        {
            get
            {
                return caption;
            }
            set
            {
                caption = value;
            }
        }

        public int Max
        {
            get
            {
                return ProgressBar_Main.Maximum;
            }

            set
            {
                ProgressBar_Main.Maximum = value;
            }
        }

        public void Update(string text, bool increment = true)
        {
            string text_Temp = text;
            if (text_Temp == null)
                text_Temp = string.Empty;

            if (increment)
            {
                ProgressBar_Main.PerformStep();
                caption = text_Temp;
                text_Temp = string.Empty;
            }

            // Once the user has asked to cancel, keep the "Cancelling..." message on screen rather than
            // overwriting it with the next step's caption; still pump messages so the form stays responsive.
            if (CancellationRequested)
            {
                Application.DoEvents();
                return;
            }

            // TimeSpan "mm" is the minutes component, so a plain mm:ss wraps back to 00:00 after an hour -
            // and a full-year TAS run can exceed that. Promote to h:mm:ss once past the hour.
            TimeSpan elapsedTimeSpan = stopwatch.Elapsed;
            string elapsed = elapsedTimeSpan.TotalHours >= 1.0
                ? string.Format("{0}:{1:00}:{2:00}", (int)elapsedTimeSpan.TotalHours, elapsedTimeSpan.Minutes, elapsedTimeSpan.Seconds)
                : string.Format("{0:00}:{1:00}", elapsedTimeSpan.Minutes, elapsedTimeSpan.Seconds);

            // Counter and elapsed lead so a long caption cannot push them out of the fixed-width label; the
            // caption and any detail are what get ellipsised. maxLength is only a coarse guard against
            // pathological strings - actual overflow is handled width-aware by Label_Description.AutoEllipsis.
            text_Temp = "[" + ProgressBar_Main.Value + "/" + ProgressBar_Main.Maximum + "] " + elapsed + " " + caption + " " + text_Temp;

            if (text_Temp.Length > maxLength)
                text_Temp = text_Temp.Substring(0, maxLength);

            Label_Description.Text = text_Temp;

            Refresh();
            BringToFront();
            Focus();
            Application.DoEvents();
        }

        private void SimpleProgressForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Threading.Thread.Sleep(1000);
        }

        private void SimpleProgressForm_Load(object sender, EventArgs e)
        {
            ProgressBar_Main.Refresh();
        }
    }
}
