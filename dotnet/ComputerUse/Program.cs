using Microsoft.Extensions.DependencyInjection;

namespace ComputerUse;

public static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Set up dependency injection
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<StatusReporter>();
            services.AddTransient<MainForm>();

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Run the application
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Arcadia Computer Use Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
