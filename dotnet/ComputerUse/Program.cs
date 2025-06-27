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

            // Special handling for run command - show warning
            if (command is RunCommand)
            {
                var result = MessageBox.Show(
                    "The AI is about to take control of this computer. This will involve taking screenshots, clicking, typing, and other interactions with your desktop.\n\nDo you want to proceed?",
                    "AI Computer Control Warning",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Set up dependency injection
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<StatusReporter>();
            services.AddSingleton<SafetyManager>();
            services.AddSingleton<ScreenUse>();
            services.AddSingleton<MouseUse>();
            services.AddSingleton<KeyboardUse>();
            services.AddSingleton<WindowWalker>();
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
            "confirm-screenshot" => ParseConfirmScreenshotCommand(args),
            "confirm-click" => ParseConfirmClickCommand(args),
            "confirm-type" => ParseConfirmTypeCommand(args),
            "screenshot" => ParseScreenshotCommand(args),
            "mouse-click" => ParseMouseClickCommand(args),
            "key-press" => ParseKeyPressCommand(args),
            "type" => ParseTypeCommand(args),
            "run" => ParseRunCommand(args),
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

    private static ConfirmScreenshotCommand ParseConfirmScreenshotCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection().AddSingleton<SafetyManager>().BuildServiceProvider();

        var command = new ConfirmScreenshotCommand(serviceProvider.GetRequiredService<SafetyManager>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--x":
                    if (++i >= args.Length || !int.TryParse(args[i], out int x))
                        throw new ArgumentException("Invalid --x parameter");
                    command.X = x;
                    break;
                case "--y":
                    if (++i >= args.Length || !int.TryParse(args[i], out int y))
                        throw new ArgumentException("Invalid --y parameter");
                    command.Y = y;
                    break;
                case "--w":
                    if (++i >= args.Length || !int.TryParse(args[i], out int w))
                        throw new ArgumentException("Invalid --w parameter");
                    command.Width = w;
                    break;
                case "--h":
                    if (++i >= args.Length || !int.TryParse(args[i], out int h))
                        throw new ArgumentException("Invalid --h parameter");
                    command.Height = h;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        return command;
    }

    private static ConfirmClickCommand ParseConfirmClickCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection().AddSingleton<SafetyManager>().BuildServiceProvider();

        var command = new ConfirmClickCommand(serviceProvider.GetRequiredService<SafetyManager>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--x":
                    if (++i >= args.Length || !int.TryParse(args[i], out int x))
                        throw new ArgumentException("Invalid --x parameter");
                    command.X = x;
                    break;
                case "--y":
                    if (++i >= args.Length || !int.TryParse(args[i], out int y))
                        throw new ArgumentException("Invalid --y parameter");
                    command.Y = y;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        return command;
    }

    private static ConfirmTypeCommand ParseConfirmTypeCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection().AddSingleton<SafetyManager>().BuildServiceProvider();

        var command = new ConfirmTypeCommand(serviceProvider.GetRequiredService<SafetyManager>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--text":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --text parameter value");
                    command.Text = args[i];
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(command.Text))
        {
            throw new ArgumentException("--text parameter is required");
        }

        return command;
    }

    private static ScreenshotCommand ParseScreenshotCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<SafetyManager>()
            .AddSingleton<ScreenUse>()
            .BuildServiceProvider();

        var command = new ScreenshotCommand(serviceProvider.GetRequiredService<ScreenUse>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--zoomPath":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --zoomPath parameter value");
                    command.ZoomPathString = args[i];
                    break;
                case "--outputFile":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --outputFile parameter value");
                    command.OutputFile = args[i];
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(command.OutputFile))
        {
            throw new ArgumentException("--outputFile parameter is required");
        }

        return command;
    }

    private static MouseClickCommand ParseMouseClickCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<SafetyManager>()
            .AddSingleton<MouseUse>()
            .BuildServiceProvider();

        var command = new MouseClickCommand(serviceProvider.GetRequiredService<MouseUse>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--zoomPath":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --zoomPath parameter value");
                    command.ZoomPathString = args[i];
                    break;
                case "--button":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --button parameter value");
                    command.Button = args[i];
                    break;
                case "--double":
                    command.Double = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(command.ZoomPathString))
        {
            throw new ArgumentException("--zoomPath parameter is required");
        }

        if (string.IsNullOrEmpty(command.Button))
        {
            throw new ArgumentException("--button parameter is required");
        }

        return command;
    }

    private static KeyPressCommand ParseKeyPressCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<SafetyManager>()
            .AddSingleton<KeyboardUse>()
            .BuildServiceProvider();

        var command = new KeyPressCommand(serviceProvider.GetRequiredService<KeyboardUse>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--key":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --key parameter value");
                    command.Key = args[i];
                    break;
                case "--shift":
                    command.Shift = true;
                    break;
                case "--ctrl":
                    command.Ctrl = true;
                    break;
                case "--alt":
                    command.Alt = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(command.Key))
        {
            throw new ArgumentException("--key parameter is required");
        }

        return command;
    }

    private static TypeCommand ParseTypeCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<SafetyManager>()
            .AddSingleton<KeyboardUse>()
            .BuildServiceProvider();

        var command = new TypeCommand(serviceProvider.GetRequiredService<KeyboardUse>());

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--text":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --text parameter value");
                    command.Text = args[i];
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(command.Text))
        {
            throw new ArgumentException("--text parameter is required");
        }

        return command;
    }

    private static RunCommand ParseRunCommand(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<StatusReporter>()
            .AddSingleton<SafetyManager>()
            .AddSingleton<ScreenUse>()
            .AddSingleton<MouseUse>()
            .AddSingleton<KeyboardUse>()
            .AddSingleton<WindowWalker>()
            .BuildServiceProvider();

        var command = new RunCommand(
            serviceProvider.GetRequiredService<ScreenUse>(),
            serviceProvider.GetRequiredService<MouseUse>(),
            serviceProvider.GetRequiredService<KeyboardUse>(),
            serviceProvider.GetRequiredService<WindowWalker>(),
            serviceProvider.GetRequiredService<StatusReporter>()
        );

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--configFile":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --configFile parameter value");
                    command.ConfigFile = args[i];
                    break;
                case "--promptFile":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --promptFile parameter value");
                    command.PromptFile = args[i];
                    break;
                case "--storageFolder":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --storageFolder parameter value");
                    command.StorageFolder = args[i];
                    break;
                case "--outputFile":
                    if (++i >= args.Length)
                        throw new ArgumentException("Missing --outputFile parameter value");
                    command.OutputFile = args[i];
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter: {args[i]}");
            }
        }

        // Validate required parameters
        if (string.IsNullOrEmpty(command.ConfigFile))
            throw new ArgumentException("--configFile parameter is required");
        if (string.IsNullOrEmpty(command.PromptFile))
            throw new ArgumentException("--promptFile parameter is required");
        if (string.IsNullOrEmpty(command.StorageFolder))
            throw new ArgumentException("--storageFolder parameter is required");
        if (string.IsNullOrEmpty(command.OutputFile))
            throw new ArgumentException("--outputFile parameter is required");

        return command;
    }
}
