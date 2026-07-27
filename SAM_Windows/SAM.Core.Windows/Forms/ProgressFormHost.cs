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

        /// <summary>
        /// Volatile because the dialog thread reads it while starting up. The constructor's wait is bounded,
        /// so a caller can be handed the host — and finish the job and dispose it — before this thread has a
        /// window handle. Without this the thread would go on to open a topmost dialog that nothing is left to
        /// close, leaving it stranded over the host application for the rest of the session.
        /// </summary>
        private volatile bool disposed;

        /// <summary>
        /// Whatever killed the dialog thread, if anything. Rethrown by the constructor when it happened during
        /// startup; if it happened later, inside the message loop, the constructor has already returned and it
        /// is surfaced through <see cref="Exception"/> instead. Either way it is never allowed to escape the
        /// thread itself.
        /// </summary>
        private volatile Exception exception_Startup;

        /// <summary>
        /// Signalled when the dialog is up, and again when its thread exits. A field rather than a local
        /// disposed by the constructor: the dialog thread still signals it after <c>Application.Run</c>
        /// returns, and setting a disposed <see cref="ManualResetEventSlim"/> throws on a background thread
        /// with no catch above it, which takes the whole host process down. Disposed in
        /// <see cref="Dispose"/>, after the thread has been joined.
        /// </summary>
        private readonly ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(false);

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
            thread = new Thread(() =>
            {
                ProgressForm progressForm_Temp = null;

                // Everything on this thread is inside the try, construction included: an exception escaping a
                // raw thread terminates the process under the default .NET policy, and a progress dialog must
                // never be able to take the host application down. Constructing the form can throw on its own
                // (a negative max, for one), which happens before any of the code below runs.
                try
                {
                    progressForm_Temp = new ProgressForm(name, max, false)
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

                    progressForm_Temp.Load += (s, e) =>
                    {
                        Release();

                        // Disposed between the check below and getting a handle: close now that there is a
                        // loop to close. Together with that check this leaves no window in which the dialog
                        // can open and stay open.
                        if (disposed)
                        {
                            progressForm_Temp.Close();
                        }
                    };

                    // Set before the loop starts so the caller never sees a null form once it is released.
                    progressForm = progressForm_Temp;

                    // Already disposed while this thread was starting up: never open the window at all.
                    if (!disposed)
                    {
                        Application.Run(progressForm_Temp);
                    }
                }
                catch (Exception exception)
                {
                    exception_Startup = exception;
                }
                finally
                {
                    // Release the caller even if the form failed before Load, rather than making it wait out
                    // the timeout below.
                    Release();

                    if (progressForm_Temp != null)
                    {
                        progressForm_Temp.Dispose();
                    }
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

            if (exception_Startup != null)
            {
                // The dialog never came up, so hand the caller a failure rather than a live-looking host that
                // silently reports nothing. Construction failed, so nothing will call Dispose - clean up here.
                disposed = true;
                thread.Join(5000);
                manualResetEventSlim.Dispose();

                // Rethrow preserving the original stack, so the real cause is not replaced by this line.
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception_Startup).Throw();
            }
        }

        /// <summary>
        /// Signals the readiness event, tolerating a Dispose that has already run — the thread signals once
        /// more on its way out, and if a join timed out that can land after disposal. An unhandled exception
        /// on this thread would terminate the host process, so it is swallowed deliberately.
        /// </summary>
        private void Release()
        {
            try
            {
                manualResetEventSlim.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Non-null when the dialog thread died of an exception raised inside its message loop, after the
        /// constructor had already returned. The dialog is gone; the job it was reporting on is unaffected and
        /// keeps running, which is why this is reported rather than thrown.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return exception_Startup;
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

            // After the join, so the dialog thread cannot still be signalling it. Release() covers the case
            // where that join timed out and the thread is somehow still alive.
            manualResetEventSlim.Dispose();
        }
    }
}
