using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using AvalonEdit.Highlighting.Xshd;
using AvalonEdit.Utils;

namespace AvalonEdit.Highlighting;

public class HighlightingManager : IHighlightingDefinitionReferenceResolver
{
    private sealed class DelayLoadedHighlightingDefinition : IHighlightingDefinition
    {
        private readonly object                        lockObj = new();
        private readonly string                        name;
        private          Func<IHighlightingDefinition> lazyLoadingFunction;
        private          IHighlightingDefinition       definition;
        private          Exception                     storedException;

        public DelayLoadedHighlightingDefinition(string name, Func<IHighlightingDefinition> lazyLoadingFunction)
        {
            this.name                = name;
            this.lazyLoadingFunction = lazyLoadingFunction;
        }

        public string Name
        {
            get
            {
                if (name != null) return name;
                return GetDefinition().Name;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception will be rethrown")]
        private IHighlightingDefinition GetDefinition()
        {
            Func<IHighlightingDefinition> func;
            lock (lockObj)
            {
                if (definition != null) return definition;
                func = lazyLoadingFunction;
            }

            Exception               exception = null;
            IHighlightingDefinition def       = null;
            try
            {
                using (var busyLock = BusyManager.Enter(this))
                {
                    if (!busyLock.Success) throw new InvalidOperationException("Tried to create delay-loaded highlighting definition recursively. Make sure the are no cyclic references between the highlighting definitions.");
                    def = func();
                }

                if (def == null) throw new InvalidOperationException("Function for delay-loading highlighting definition returned null");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            lock (lockObj)
            {
                lazyLoadingFunction = null;
                if (definition == null && storedException == null)
                {
                    definition      = def;
                    storedException = exception;
                }

                if (storedException != null) throw new HighlightingDefinitionInvalidException("Error delay-loading highlighting definition", storedException);
                return definition;
            }
        }

        public HighlightingRuleSet MainRuleSet => GetDefinition().MainRuleSet;

        public HighlightingRuleSet GetNamedRuleSet(string name)
        {
            return GetDefinition().GetNamedRuleSet(name);
        }

        public HighlightingColor GetNamedColor(string name)
        {
            return GetDefinition().GetNamedColor(name);
        }

        public IEnumerable<HighlightingColor> NamedHighlightingColors => GetDefinition().NamedHighlightingColors;

        public override string ToString()
        {
            return Name;
        }

        public IDictionary<string, string> Properties => GetDefinition().Properties;
    }

    private readonly object                                      lockObj                  = new();
    private          Dictionary<string, IHighlightingDefinition> highlightingsByName      = new();
    private          Dictionary<string, IHighlightingDefinition> highlightingsByExtension = new(StringComparer.OrdinalIgnoreCase);
    private          List<IHighlightingDefinition>               allHighlightings         = new();

    public IHighlightingDefinition GetDefinition(string name)
    {
        lock (lockObj)
        {
            return highlightingsByName.TryGetValue(name, out var rh) ? rh : null;
        }
    }

    public ReadOnlyCollection<IHighlightingDefinition> HighlightingDefinitions
    {
        get
        {
            lock (lockObj)
            {
                return Array.AsReadOnly(allHighlightings.ToArray());
            }
        }
    }

    public IHighlightingDefinition GetDefinitionByExtension(string extension)
    {
        lock (lockObj)
        {
            return highlightingsByExtension.TryGetValue(extension, out var rh) ? rh : null;
        }
    }

    public void RegisterHighlighting(string name, string[] extensions, IHighlightingDefinition highlighting)
    {
        if (highlighting == null) throw new ArgumentNullException(nameof(highlighting));

        lock (lockObj)
        {
            allHighlightings.Add(highlighting);
            if (name != null) highlightingsByName[name] = highlighting;
            if (extensions != null)
                foreach (var ext in extensions)
                    highlightingsByExtension[ext] = highlighting;
        }
    }

    public void RegisterHighlighting(string name, string[] extensions, Func<IHighlightingDefinition> lazyLoadedHighlighting)
    {
        if (lazyLoadedHighlighting == null) throw new ArgumentNullException(nameof(lazyLoadedHighlighting));
        RegisterHighlighting(name, extensions, new DelayLoadedHighlightingDefinition(name, lazyLoadedHighlighting));
    }

    public static HighlightingManager Instance => DefaultHighlightingManager.Instance;

    internal sealed class DefaultHighlightingManager : HighlightingManager
    {
        public new static readonly DefaultHighlightingManager Instance = new();

        public DefaultHighlightingManager()
        {
            Resources.RegisterBuiltInHighlightings(this);
        }

        internal void RegisterHighlighting(string name, string[] extensions, string resourceName)
        {
            try
            {
#if DEBUG
                // don't use lazy-loading in debug builds, show errors immediately
                XshdSyntaxDefinition xshd;
                using (var s = Resources.OpenStream(resourceName))
                {
                    using (var reader = new XmlTextReader(s))
                    {
                        xshd = HighlightingLoader.LoadXshd(reader, false);
                    }
                }

                Debug.Assert(name == xshd.Name);
                if (extensions != null) Debug.Assert(extensions.SequenceEqual(xshd.Extensions));
                else Debug.Assert(xshd.Extensions.Count == 0);

                // round-trip xshd:
                //					string resourceFileName = Path.Combine(Path.GetTempPath(), resourceName);
                //					using (XmlTextWriter writer = new XmlTextWriter(resourceFileName, System.Text.Encoding.UTF8)) {
                //						writer.Formatting = Formatting.Indented;
                //						new Xshd.SaveXshdVisitor(writer).WriteDefinition(xshd);
                //					}
                //					using (FileStream fs = File.Create(resourceFileName + ".bin")) {
                //						new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(fs, xshd);
                //					}
                //					using (FileStream fs = File.Create(resourceFileName + ".compiled")) {
                //						new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(fs, Xshd.HighlightingLoader.Load(xshd, this));
                //					}

                RegisterHighlighting(name, extensions, HighlightingLoader.Load(xshd, this));
#else
                RegisterHighlighting(name, extensions, LoadHighlighting(resourceName));
#endif
            }
            catch (HighlightingDefinitionInvalidException ex)
            {
                throw new InvalidOperationException("The built-in highlighting '" + name + "' is invalid.", ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "LoadHighlighting is used only in release builds")]
        private Func<IHighlightingDefinition> LoadHighlighting(string resourceName)
        {
            Func<IHighlightingDefinition> func = delegate
            {
                XshdSyntaxDefinition xshd;
                using (var s = Resources.OpenStream(resourceName))
                {
                    using (var reader = new XmlTextReader(s))
                    {
                        xshd = HighlightingLoader.LoadXshd(reader, true);
                    }
                }

                return HighlightingLoader.Load(xshd, this);
            };
            return func;
        }
    }
}