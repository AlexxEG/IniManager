﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace System.Ini
{
    public class IniManager
    {
        OrderedDictionary p_Sections;

        /// <summary>
        /// Gets or sets whether changes is automatically saved.
        /// </summary>
        public bool AutoSave { get; set; }
        public int CountProperties
        {
            get
            {
                int count = 0;

                foreach (DictionaryEntry entry in p_Sections)
                {
                    count += (entry.Value as IniSection).Count;
                }

                return count;
            }
        }
        public int CountSections
        {
            get { return p_Sections.Count; }
        }
        /// <summary>
        /// Gets the filename of the current configuration file.
        /// </summary>
        public string Filename { get; private set; }
        /// <summary>
        /// Gets the sections of the configuration file with properties as read-only.
        /// </summary>
        public OrderedDictionary Sections
        {
            get { return p_Sections.AsReadOnly(); }
        }

        /// <summary>
        /// Initializes a new instance of the System.Ini.IniManager class. Does not load until Load method is called.
        /// </summary>
        /// <param name="filename">The complete file path to write and read configuration file.</param>
        public IniManager(string filename)
        {
            this.AutoSave = false;
            this.Filename = filename;
            this.p_Sections = new OrderedDictionary();
        }

        /// <summary>
        /// Backs up the configuration file.
        /// </summary>
        /// <param name="file">Path to save backup file to.</param>
        /// <param name="applyChanges">True if changes should be written, false to just copy the configuration file.</param>
        public void Backup(string file, bool applyChanges)
        {
            if (applyChanges)
            {
                this.Save(file);
            }
            else
            {
                if (!File.Exists(this.Filename))
                    throw new FileNotFoundException("INI file not found", this.Filename);

                File.Copy(this.Filename, file);
            }
        }

        /// <summary>
        /// Deletes property from section in configuration file.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        public void Delete(string section, string key)
        {
            IniSection properties = this.GetSection(section);

            if (properties == null)
                return;

            /* If the key is the last property, might as well delete section. */
            if (properties.Contains(key) && properties.Count == 1)
            {
                p_Sections.Remove(section);
            }
            else
            {
                properties.Remove(key);
            }
        }

        /// <summary>
        /// Deletes section in configuration file.
        /// </summary>
        /// <param name="section">The section to delete.</param>
        public void DeleteSection(string section)
        {
            if (p_Sections.Contains(section))
            {
                p_Sections.Remove(section);
            }
        }

        /// <summary>
        /// Returns the property object, or null if it couldn't be found.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        public IniProperty Get(string section, string key)
        {
            return (p_Sections[section] as IniSection).Get(key);
        }

        /// <summary>
        /// Returns the property value.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="defaultValue">The value to return if the property couldn't be found.</param>
        public object GetValue(string section, string key, object defaultValue)
        {
            if (!p_Sections.Contains(section) ||
                !this.GetSection(section).Contains(key))
            {
                return defaultValue;
            }

            return this.GetSection(section).Get(key).Value;
        }

        /// <summary>
        /// Returns the property boolean value.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="defaultValue">The value to return if the property couldn't be found.</param>
        public bool GetBoolean(string section, string key, bool defaultValue)
        {
            return bool.Parse(this.GetValue(section, key, defaultValue).ToString());
        }

        /// <summary>
        /// Returns the property integer value.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="defaultValue">The value to return if the property couldn't be found.</param>
        public int GetInteger(string section, string key, int defaultValue)
        {
            return int.Parse(this.GetValue(section, key, defaultValue).ToString());
        }

        /// <summary>
        /// Returns the property long value.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="defaultValue">The value to return if the property couldn't be found.</param>
        public long GetLong(string section, string key, long defaultValue)
        {
            return long.Parse(this.GetValue(section, key, defaultValue).ToString());
        }

        /// <summary>
        /// Returns the properties from the given section.
        /// </summary>
        /// <param name="section">Section to return properties from.</param>
        public IniSection GetSection(string section)
        {
            return (IniSection)p_Sections[section];
        }

        /// <summary>
        /// Returns list of all section names, not including the brackets.
        /// </summary>
        public ICollection<string> GetSections()
        {
            return (ICollection<string>)p_Sections.Keys;
        }

        /// <summary>
        /// Returns the property string value.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="defaultValue">The value to return if the property couldn't be found.</param>
        public string GetString(string section, string key, string defaultValue)
        {
            return this.GetValue(section, key, defaultValue).ToString();
        }

        /// <summary>
        /// Loads the properties from the configuration file.
        /// </summary>
        public void Load()
        {
            if (!File.Exists(this.Filename))
                return;

            using (var reader = new StreamReader(this.Filename))
            {
                string line;
                IniSection section = null;

                while ((line = reader.ReadLine()) != null)
                {
                    /* Empty line. */
                    if (string.IsNullOrEmpty(line))
                    {
                        section.Add(new IniProperty(section.Name, IniType.EmptyLine, line));
                    }
                    /* Comment line. */
                    else if (line.TrimStart().StartsWith(";") || line.TrimStart().StartsWith("#"))
                    {
                        section.Add(new IniProperty(section.Name, IniType.Comment, line));
                    }
                    /* Section line. */
                    else if (line.TrimStart().StartsWith("[") && line.TrimEnd().EndsWith("]"))
                    {
                        string name = line.Substring(line.IndexOf("[") + 1, line.IndexOf("]") - line.IndexOf("[") - 1);

                        section = new IniSection(name);

                        p_Sections.Add(name, section);
                    }
                    /* Valid property line.
                     * (NOTE: Placing this check higher up is faster, but doesn't account for '=' in comments.) */
                    else if (line.Contains("="))
                    {
                        string[] split = line.Split('=');

                        section.Add(new IniProperty(section.Name, split[0], split[1]));
                    }
                    /* Invalid line. */
                    else
                    {
                        section.Add(new IniProperty(section.Name, IniType.Invalid, line));
                    }
                }
            }
        }

        /// <summary>
        /// Adds or sets the value of the property.
        /// </summary>
        /// <param name="section">The section of the property.</param>
        /// <param name="key">The key of the property.</param>
        /// <param name="value">The property value.</param>
        public void Put(string section, string key, object value)
        {
            IniSection sect = null;

            if (p_Sections.Contains(section))
            {
                sect = this.GetSection(section);
            }
            else
            {
                if (p_Sections.Count > 0)
                {
                    IniSection lastSect = (IniSection)p_Sections[p_Sections.Count - 1];

                    /* Insert empy line? */
                    if (!string.IsNullOrEmpty(lastSect[lastSect.Count - 1].Value.ToString()))
                    {
                        lastSect.Add(new IniProperty(section, IniType.EmptyLine, ""));
                    }
                }

                sect = new IniSection(section);

                p_Sections.Add(section, sect);
            }

            if (sect.Contains(key))
            {
                sect.Get(key).Value = value;
            }
            else
            {
                sect.Add(new IniProperty(section, key, value));
            }

            if (this.AutoSave)
            {
                this.Save();
            }
        }

        /// <summary>
        /// Saves the properties to the configuration file.
        /// </summary>
        public void Save()
        {
            this.Save(this.Filename);
        }

        /// <summary>
        /// Saves the properties to the given file.
        /// </summary>
        /// <param name="file">The file to save the properties to.</param>
        private void Save(string file)
        {
            using (var writer = new StreamWriter(file))
            {
                foreach (DictionaryEntry pair in p_Sections)
                {
                    writer.WriteLine("[{0}]", pair.Key);

                    foreach (IniProperty property in (pair.Value as IniSection).GetAll())
                    {
                        /* Respect comment/empty/invalid lines. */
                        if (property.Type != IniType.Property)
                        {
                            writer.WriteLine(property.Value.ToString());
                        }
                        else
                        {
                            writer.WriteLine("{0}={1}", property.Key, property.Value);
                        }
                    }
                }
            }
        }
    }
}