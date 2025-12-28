using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.Command;

namespace Hypostasis.Dalamud;

[AttributeUsage(AttributeTargets.Method)]
public class PluginCommandAttribute(params string[] commands) : Attribute
{
    public string[] Commands { get; init; } = commands;
    public string HelpMessage { get; init; } = string.Empty;
    public bool ShowInHelp { get; init; } = true;
}

public sealed class PluginCommandManager : IDisposable
{
    private readonly HashSet<string> pluginCommands = [];

    public PluginCommandManager(object o)
    {
        foreach (var method in o.GetType().GetAllMethods())
            AddPluginCommandMethod(o, method);
    }

    private void AddPluginCommandMethod(object o, MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<PluginCommandAttribute>();
        if (attribute == null) return;

        var handlerDelegate = (IReadOnlyCommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(IReadOnlyCommandInfo.HandlerDelegate), o, method);
        var commandInfo = new CommandInfo(handlerDelegate)
        {
            HelpMessage = attribute.HelpMessage,
            ShowInHelp = attribute.ShowInHelp,
        };

        foreach (var command in attribute.Commands)
        {
            if (DalamudApi.CommandManager.AddHandler(command, commandInfo))
                pluginCommands.Add(command);
        }
    }

    public void Dispose()
    {
        foreach (var command in pluginCommands)
            DalamudApi.CommandManager.RemoveHandler(command);
    }
}