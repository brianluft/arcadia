using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ComputerUse
{
    public static class FormHider
    {
        public static void Do(Action action)
        {
            // Collect all currently open forms and their visibility states
            var formsToRestore = new List<(Form form, bool wasVisible)>();

            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            {
                formsToRestore.Add((form, form.Visible));
                if (form.Visible)
                {
                    form.Hide();
                }
            }

            // Wait 500ms for the forms to be properly hidden
            Thread.Sleep(500);

            try
            {
                // Execute the provided action
                action();
            }
            finally
            {
                // Restore the visibility of forms that were originally visible
                foreach (var (form, wasVisible) in formsToRestore)
                {
                    if (wasVisible && !form.IsDisposed)
                    {
                        form.Show();
                    }
                }
            }
        }
    }
}
