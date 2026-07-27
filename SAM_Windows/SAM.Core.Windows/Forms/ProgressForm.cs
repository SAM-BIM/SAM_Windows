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
        /// Opt-in: shows the Cancel button. Off by default so existing callers of the shared form are
        /// unaffected. Callers that set this should honour <see cref="CancellationRequested"/> (or subscribe
        /// to <see cref="CancelRequested"/>) between steps.
        /// </summary>
        public bool Cancellable
        {
            get
            {
                return Button_Cancel.Visible;
            }
            set
            {
                Button_Cancel.Visible = value;
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

            string elapsed = stopwatch.Elapsed.ToString(@"mm\:ss");
            text_Temp = caption + " [" + ProgressBar_Main.Value + "/" + ProgressBar_Main.Maximum + "] " + elapsed + " " + text_Temp;

            if (text_Temp.Length > maxLength)
                text_Temp = text_Temp.Substring(0, maxLength) + "...";

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
