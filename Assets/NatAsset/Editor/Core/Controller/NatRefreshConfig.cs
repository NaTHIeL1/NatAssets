// using System;
// using System.Collections.Generic;
//
// namespace NATFrameWork.NatAsset.Editor
// {
//     public static class NatRefreshConfig
//     {
//         internal static void RefreshFromThisItem(TreeViewItem<BundleInventory> treeViewItem)
//         {
//             // ParticalSwitchBuild(treeViewItem);
//             // Reload();
//             // Repaint();
//         }
//
//         internal static void RefreshItemInclude(TreeViewItem<BundleInventory> itemTreeView)
//         {
//             BundleInventory data = itemTreeView.data;
//             if (data.IncludePackage)
//             {
//                 List<BundleInventory> bundleInventories = data.BundleInventories;
//                 for (int i = 0; i < bundleInventories.Count; i++)
//                 {
//                     BundleInventory item = bundleInventories[i];
//                     item.IncludePackage = data.IncludePackage;
//                     RefreshDownStreamInclude(item, data.IncludePackage);
//                 }
//
//                 CheckInclude(itemTreeView);
//             }
//             else
//             {
//                 RefreshUpStreamInclude(itemTreeView, data.IncludePackage);
//                 RefreshDownStreamInclude(data, data.IncludePackage);
//             }
//         }
//
//         internal static void IncludeAll(bool include)
//         {
//             List<BundleInventory> bundleInventories = NatAssetBuildSetting.Instance.BundleInventories;
//             for (int i = 0; i < bundleInventories.Count; i++)
//             {
//                 BundleInventory bundleInventory = bundleInventories[i];
//                 bundleInventory.IncludePackage = include;
//                 IncludeDG(bundleInventory,include);
//             }
//         }
//         
//         private static void RefreshDownStreamInclude(BundleInventory bundleInventory, bool isInclude)
//         {
//             bundleInventory.IncludePackage = isInclude;
//             List<BundleInventory> bundleInventories = bundleInventory.BundleInventories;
//             if (bundleInventories == null) return;
//             for (int i = 0; i < bundleInventories.Count; i++)
//             {
//                 BundleInventory tempItem = bundleInventories[i];
//                 tempItem.IncludePackage = isInclude;
//                 RefreshDownStreamInclude(tempItem, isInclude);
//             }
//         }
//
//         private static void RefreshUpStreamInclude(TreeViewItem<BundleInventory> itemTreeView, bool isInclude)
//         {
//             if (itemTreeView == null) return;
//             itemTreeView.data.IncludePackage = isInclude;
//             TreeViewItem<BundleInventory> itemParent = (TreeViewItem<BundleInventory>) itemTreeView.parent;
//             RefreshUpStreamInclude(itemParent, isInclude);
//         }
//
//         private static void CheckInclude(TreeViewItem<BundleInventory> itemTreeView)
//         {
//             BundleInventory bundleInventory = itemTreeView.data;
//             List<BundleInventory> bundleInventories = bundleInventory.BundleInventories;
//             if (bundleInventories == null) return;
//             bool canInclude = true;
//             for (int i = 0; i < bundleInventories.Count; i++)
//             {
//                 if (!bundleInventories[i].IncludePackage)
//                 {
//                     canInclude = false;
//                     break;
//                 }
//             }
//
//             bundleInventory.IncludePackage = canInclude;
//             TreeViewItem<BundleInventory> itemParent = (TreeViewItem<BundleInventory>) itemTreeView.parent;
//             if (itemParent == null) return;
//             CheckInclude(itemParent);
//         }
//
//         private static void IncludeDG(BundleInventory bundleInventory, bool include)
//         {
//             List<BundleInventory> bundleInventories = bundleInventory.BundleInventories;
//             for (int i = 0; i < bundleInventories.Count; i++)
//             {
//                 BundleInventory temp = bundleInventories[i];
//                 temp.IncludePackage = include;
//                 IncludeDG(temp, include);
//             }
//         }
//     }
// }