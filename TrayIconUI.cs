using System;
using System.Windows.Forms;
using Woof.SystemEx;

namespace NBSP {

    /// <summary>
    /// Minimalistic Tray Icon UI.
    /// </summary>
    class TrayIconUI : ApplicationContext {
        
        /// <summary>
        /// Text to display as a tooltip for the tray icon.
        /// </summary>
        const string Description = "NBSP - press Ctrl+Space to insert non-breaking space.";

        /// <summary>
        /// Unicode character for non-breaking space.
        /// </summary>
        const string NBSP = "\u00A0";

        /// <summary>
        /// Creates the tray icon, initializes menu and hooks global keyboard shortcut handler.
        /// </summary>
        public TrayIconUI() {
            TrayMenu.Items.Add("Exit", null, MenuExit);
            TrayIcon.ContextMenuStrip = TrayMenu;
            GlobalInput.KeyDown += GlobalInput_KeyDown;
        }

        /// <summary>
        /// Global keyboard shortcut handler.
        /// </summary>
        /// <param name="sender">This.</param>
        /// <param name="e">Event arguments.</param>
        private void GlobalInput_KeyDown(object sender, GlobalKeyEventArgs e) {
            if (e.KeyData == (Keys.Control | Keys.Space)) {
                e.IsHandled = true;
                GlobalInput.Paste(NBSP);
            }
        }

        /// <summary>
        /// Exits application and disposes resources.
        /// </summary>
        /// <param name="sender">This.</param>
        /// <param name="e">Empty event arguments.</param>
        private void MenuExit(object sender, EventArgs e) => OnMainFormClosed(sender, e);

        /// <summary>
        /// Disposes disposable resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                GlobalInput.Dispose();
                TrayIcon.Dispose();
            }
        }

        /// <summary>
        /// Application tray icon.
        /// </summary>
        private readonly NotifyIcon TrayIcon = new NotifyIcon { Icon = Properties.Resources.NBSP_icon, Visible = true, Text = Description };

        /// <summary>
        /// Application menu.
        /// </summary>
        private readonly ContextMenuStrip TrayMenu = new ContextMenuStrip();

        /// <summary>
        /// <see cref="GlobalInput"/> instance.
        /// </summary>
        private readonly GlobalInput GlobalInput = new GlobalInput();

    }

}