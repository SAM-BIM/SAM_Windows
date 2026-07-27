// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Threading;
using System.Windows.Forms;

namespace SAM.Core.Windows.Forms
{
    /// <summary>
    /// Hosts a <see cref="ProgressForm"/> on a dedicated UI thread running its own message loop, so the dialog
    /// keeps painting and its Cancel button keeps accepting clicks while the CALLING thread is blocked inside a
    /// long, uninterruptible call — a TAS COM simulate, say.
    /// <para>
    /// This exists because a Cancel button on the blocked thread's own form cannot be relied on. Windows ghosts
    /// a top-level window whose thread has not pumped for a few seconds (the "Not Responding" overlay) and
    /// silently discards clicks on the ghost, so the click is never queued and the later
    /// <see cref="Application.DoEvents"/> has nothing to deliver. The user clicks Cancel, sees nothing happen,
    /// and the run continues. Moving only the dialog to its own thread fixes that without moving any COM work.
    /// </para>
    /// <para>
    /// The job itself stays on the caller's thread and is NOT interrupted — cancellation is still cooperative
    /// and observed between steps. What changes is that the request is always recorded the instant it is made.
    /// </para>
    /// <para>
    /// Only the members on this class may be touched from the calling thread; they marshal onto the dialog's
    /// thread. <see cref="Dispose"/> closes the form and joins the thread.
    /// </para>
    /// </summary>
    public sealed class ProgressFormHost : IDisposable
    {
        private readonly Thread thread;
        private volatile ProgressForm progressForm;
        private bool disposed;

        /// <summary>
        /// Raised on the dialog's thread when the user clicks Cancel, so a handler must be safe to call from a
        /// thread other than the one running the job. Cancelling a <c>CancellationTokenSource</c> is.
        /// </summary>
        // System-qualified: SAM.Core.Windows has its own EventHandler namespace that otherwise wins here.
        public event System.EventHandler CancelRequested;

        /// <param name="name">Window title.</param>
        /// <param name="max">Number of steps the progress bar counts to.</param>
        /// <param name="cancellable">Shows the Cancel button and the note line.</param>
        /// <param name="note">Initial note text; see <see cref="ProgressForm.Note"/>.</param>
        public ProgressFormHost(string name, int max, bool cancellable, string note)
        {
            using (ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false))
            {
                thread = new Thread(() =>
                {
                    ProgressForm progressForm_Temp = new ProgressForm(name, max, false)
                    {
                        // Not owned by the host application's main window: an owner must live on the same
                        // thread as the owned form, and this one deliberately does not. TopMost keeps it in
                        // front of the frozen host instead.
                        TopMost = true,
                        StartPosition = FormStartPosition.CenterScreen,
                        OwnsMessageLoop = true,
                        Cancellable = cancellable,
                    };

                    progressForm_Temp.Note = note;
                    progressForm_Temp.CancelRequested += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);

                    // Set before the loop starts so the caller never sees a null form once it is released.
                    progressForm_Temp.Load += (s, e) => manualResetEventSlim.Set();
                    progressForm = progressForm_Temp;

                    try
                    {
                        Application.Run(progressForm_Temp);
                    }
                    finally
                    {
                        // Release the caller even if the form failed before Load, rather than making it wait
                        // out the timeout below.
                        manualResetEventSlim.Set();
                        progressForm_Temp.Dispose();
                    }
                })
                {
                    IsBackground = true,
                    Name = "sam-progress-ui",
                };

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                // Bounded: a dialog that will not come up must never hold up the job it is reporting on.
                manualResetEventSlim.Wait(5000);
            }
        }

        /// <summary>True once the user has clicked Cancel. Safe to read from the job's thread.</summary>
        public bool CancellationRequested
        {
            get
            {
                ProgressForm progressForm_Temp = progressForm;

                return progressForm_Temp != null && progressForm_Temp.CancellationRequested;
            }
        }

        /// <summary>Number of steps the progress bar counts to; set it once the count is known.</summary>
        public int Max
        {
            set
            {
                ProgressForm progressForm_Temp = progressForm;
                if (progressForm_Temp != null)
                {
                    progressForm_Temp.Max = value;
                }
            }
        }

        /// <summary>Note under the main line — say what Cancel can and cannot interrupt at this stage.</summary>
        public string Note
        {
            set
            {
                ProgressForm progressForm_Temp = progressForm;
                if (progressForm_Temp != null)
                {
                    progressForm_Temp.Note = value;
                }
            }
        }

        /// <summary>Advances the bar and shows <paramref name="description"/> as the current step.</summary>
        public void Update(string description, bool increment = true)
        {
            ProgressForm progressForm_Temp = progressForm;
            if (progressForm_Temp != null)
            {
                progressForm_Temp.Update(description, increment);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            ProgressForm progressForm_Temp = progressForm;
            if (progressForm_Temp != null)
            {
                try
                {
                    if (progressForm_Temp.IsHandleCreated && !progressForm_Temp.IsDisposed)
                    {
                        // Closing the form ends Application.Run, which ends the thread.
                        progressForm_Temp.BeginInvoke(new Action(progressForm_Temp.Close));
                    }
                }
                catch (System.ComponentModel.InvalidAsynchronousStateException)
                {
                    // the thread is already gone
                }
                catch (ObjectDisposedException)
                {
                    // same
                }
            }

            // Bounded for the same reason as the startup wait: a stuck dialog thread must not hang the host.
            // ProgressForm's FormClosing sleeps for a second, so this is never instant.
            thread?.Join(5000);

            progressForm = null;
        }
    }
}
