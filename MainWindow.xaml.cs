using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace RimXML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string rimworldAssemblyPath = @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\";
        string rimworldAssemblyName = "Assembly-CSharp.dll";
        Assembly rimworldAssembly;
        Type defType;
        Type unsavedAttribute;
        Type defaultValueAttribute;

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(rimworldAssemblyPath + rimworldAssemblyName))
                LoadAssembly(rimworldAssemblyPath + rimworldAssemblyName);
        }

        private void BtnLocateRimworld_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Assembly Files (*.dll)|*.dll";
            openFileDialog.Title = "Locate Assembly-CSharp.dll in the RimWorld\\RimWorldWin64_Data\\Managed Folder";
            openFileDialog.InitialDirectory = rimworldAssemblyPath;
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
                rimworldAssemblyPath = openFileDialog.FileName;
            LoadAssembly(rimworldAssemblyPath);
        }

        private void LoadAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                MessageBox.Show("The selected file does not exist.");
                return;
            }

            rimworldAssembly = Assembly.LoadFrom(assemblyPath);

            defType = rimworldAssembly.GetType("Verse.Def");
            List<Type> defTypes = new();

            var types = rimworldAssembly.GetTypes().OrderBy(t => t.Name).ToArray();
            for (int i = 0; i < types.Length; ++i)
            {
                var type = types[i];
                if (type.IsAssignableTo(defType))
                {
                    defTypes.Add(type);
                    var menuItem = new MenuItem { Name = "MenuItemDefType" + type.Name, Header = type.Name };
                    menuItem.Click += delegate
                    {
                        Editor.Text = GenerateDefTemplate(type);
                    };
                    MenuItemDefTypes.Items.Add(menuItem);
                }
            }
            //MessageBox.Show("Loaded assembly with " + defTypes.Count + " def types.");
        }

        private string GenerateDefTemplate(Type type)
        {
            unsavedAttribute = rimworldAssembly.GetType("Verse.UnsavedAttribute");
            defaultValueAttribute = rimworldAssembly.GetType("Verse.DefaultValueAttribute");
            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf - 8\" ?>\n<Defs>\n\t<" + type.Name + ">\n");
            var publicFields = type.GetFields();
            for (int i = 0; i < publicFields.Length; ++i)
            {
                var field = publicFields[i];
                if (field.GetCustomAttribute(unsavedAttribute) == null)
                {
                    string line = "\t\t<" + field.Name + ">";
                    var defaultValue = field.GetCustomAttribute(defaultValueAttribute);
                    if (defaultValue != null)
                        line += defaultValueAttribute.GetField("value").GetValue(defaultValue) ?? "";
                    //else if (field.FieldType != typeof(string))
                    //    line += Activator.CreateInstance(field.FieldType)?.ToString();
                    line += "</" + field.Name + ">";
                    sb.AppendLine(line);
                }
            }
            sb.Append("\t</" + type.Name + ">\n</Defs>");
            return sb.ToString();
        }
    }
}