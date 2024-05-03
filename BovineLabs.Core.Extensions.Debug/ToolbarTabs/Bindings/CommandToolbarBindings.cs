// <copyright file="CommandToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.UI;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class CommandToolbarBindings : IBindingObject<CommandToolbarBindings.Data>, INotifyBindablePropertyChanged
    {
        private readonly Dictionary<string, (ConfigVarAttribute Attribute, IConfigVarContainer ConfigVar)> configVars = new();
        private readonly string[] output = new string[5];

        private Data data;
        private string input = string.Empty;
        private int caret;

        public CommandToolbarBindings()
        {
            foreach (var config in ConfigVarManager.All)
            {
                this.configVars.Add(config.Key.Name, (config.Key, config.Value));
            }
        }

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public string Input
        {
            get => this.input;
            set
            {
                if (this.input != value)
                {
                    this.UpdateUserInput(value);
                }
            }
        }

        [CreateProperty]
        public int Caret
        {
            get => this.caret;
            set
            {
                this.caret = value;
                this.OnPropertyChanged(nameof(this.Caret));
            }
        }

        [CreateProperty]
        public bool Submitted
        {
            get => false;
            set
            {
                if (value)
                {
                    this.Submit();
                }
            }
        }

        [CreateProperty]
        public string[] Output => this.output;

        private void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        private void UpdateUserInput(string text)
        {
            var oldInput = this.input;
            this.input = text.TrimStart();

            if (string.IsNullOrWhiteSpace(this.input))
            {
                this.WriteOutput();
                return;
            }

            // Just entered a period and it didn't match anything, auto complete.
            // Ensure it's not a deletion, isn't just a single period, latest is a period, it matches anything
            if (this.input.Length > oldInput.Length &&
                this.input.Length > 1 &&
                this.input[^1] == '.' &&
                this.configVars.Keys.All(s => !s.StartsWith(this.input)))
            {
                // Get the first result
                var key = this.configVars.Keys.Where(s => s.StartsWith(this.input[..^1])).OrderBy(s => s).FirstOrDefault();
                if (key != null)
                {
                    var index = key.Remove(0, this.input.Length - 1).IndexOf('.');

                    // not found, means we're at end so add a space for convenience
                    string newInput;
                    if (index == -1)
                    {
                        newInput = $"{key} ";
                    }
                    else
                    {
                        newInput = key[..(index + this.input.Length)];
                    }

                    this.UpdateInput(newInput);
                }
            }

            if (this.configVars.TryGetValue(this.input.TrimEnd(), out var value))
            {
                this.WriteOutput(value.ConfigVar.StringValue);
                return;
            }

            var keys = this.configVars.Keys.Where(s => s.StartsWith(this.input)).OrderBy(s => s).ToArray();
            this.WriteOutput(keys);
        }

        private void UpdateInput(string value)
        {
            if (this.input != value)
            {
                this.input = value;
                this.OnPropertyChanged(nameof(this.Input));
                this.Caret = this.input.Length;
            }
        }

        private void WriteOutput(params string[] values)
        {
            for (var i = 0; i < this.output.Length; i++)
            {
                this.output[i] = string.Empty;
            }

            var length = math.min(values.Length, this.output.Length);
            for (var i = 0; i < length; i++)
            {
                this.output[i] = values[i];
            }

            this.OnPropertyChanged(nameof(this.Output));
        }

        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(this.Input))
            {
                return;
            }

            var split = this.Input.TrimEnd().Split(" ");
            switch (split.Length)
            {
                case 1:
                {
                    if (this.configVars.TryGetValue(split[0], out var variable))
                    {
                        this.UpdateInput(string.Empty);
                        this.WriteOutput(variable.ConfigVar.StringValue);
                    }

                    break;
                }

                case 2:
                {
                    if (this.configVars.TryGetValue(split[0], out var variable))
                    {
                        if (variable.Attribute.IsReadOnly)
                        {
                            this.WriteOutput("Variable ReadOnly");
                            this.UpdateInput(string.Empty);
                        }
                        else
                        {
                            try
                            {
                                var k = split[0].Split(".").Last();
                                variable.ConfigVar.StringValue = split[1];
                                this.WriteOutput($"{k} to {split[1]}");
                                this.UpdateInput(string.Empty);
                            }
                            catch
                            {
                                this.WriteOutput($"Invalid, expected {variable.ConfigVar.Type.Name}");
                            }
                        }
                    }

                    break;
                }
            }
        }

        public struct Data
        {
        }
    }
}
