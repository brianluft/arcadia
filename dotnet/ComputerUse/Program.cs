using Microsoft.Extensions.DependencyInjection;

namespace ComputerUse;

public static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Parse command line arguments
            var command = ParseArguments(args);

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
            mainForm.SetCommand(command);
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Arcadia Computer Use Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }

    private static ICommand ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No command specified. Usage: ComputerUse.exe <command> [options]");
        }

        var command = args[0].ToLowerInvariant();

        return command switch
        {
            "noop" => ParseNoopCommand(args),
            _ => throw new ArgumentException($"Unknown command: {command}"),
        };
    }

    private static NoopCommand ParseNoopCommand(string[] args)
    {
        // Noop command has no parameters
        if (args.Length > 1)
        {
            throw new ArgumentException("Noop command does not accept any parameters.");
        }

        return new NoopCommand();
    }
}
