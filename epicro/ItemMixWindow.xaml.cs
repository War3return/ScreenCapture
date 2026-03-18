using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using epicro.Models;

namespace epicro
{
    public partial class ItemMixWindow : Window
    {
        private Dictionary<string, ItemMixConfig> itemConfigs;
        private Dictionary<string, int> materialCounts = new Dictionary<string, int>();
        private ObservableCollection<KeyValuePair<string, int>> materialDisplay = new ObservableCollection<KeyValuePair<string, int>>();

        public ItemMixWindow()
        {
            InitializeComponent();
            itemConfigs = ItemMixConfigManager.GetItemMixConfigs();
            MaterialGrid.ItemsSource = materialDisplay;
            CreateUI();
        }

        private void CreateUI()
        {
            var categories = itemConfigs.Values.Select(cfg => cfg.Category).Distinct();

            foreach (var category in categories)
            {
                var expander = new Expander
                {
                    Header = category,
                    IsExpanded = false,
                    Margin = new Thickness(5)
                };

                var stack = new StackPanel();
                foreach (var pair in itemConfigs.Where(kvp => kvp.Value.Category == category))
                {
                    var cb = new CheckBox
                    {
                        Content = pair.Key,
                        Margin = new Thickness(5)
                    };
                    cb.Checked += ItemCheckChanged;
                    cb.Unchecked += ItemCheckChanged;
                    stack.Children.Add(cb);
                }

                expander.Content = stack;
                CategoryPanel.Children.Add(expander);
            }
        }

        private void ItemCheckChanged(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var itemName = checkbox.Content.ToString();

            if (!itemConfigs.ContainsKey(itemName)) return;

            var materials = itemConfigs[itemName].Materials;

            foreach (var mat in materials)
            {
                if (checkbox.IsChecked == true)
                {
                    if (!materialCounts.ContainsKey(mat))
                        materialCounts[mat] = 0;
                    materialCounts[mat]++;
                }
                else
                {
                    materialCounts[mat]--;
                    if (materialCounts[mat] <= 0)
                        materialCounts.Remove(mat);
                }
            }

            RefreshMaterialDisplay();
        }

        private void RefreshMaterialDisplay()
        {
            materialDisplay.Clear();
            foreach (var kvp in materialCounts.OrderBy(k => k.Key))
            {
                materialDisplay.Add(new KeyValuePair<string, int>(kvp.Key, kvp.Value));
            }
        }
    }
}
