﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClassLibraryCommon;
using NonVisuals;
using DCSFlightpanels.Properties;
using DCSFlightpanels.Bills;
using DCSFlightpanels.CustomControls;
using DCSFlightpanels.PanelUserControls;
using DCSFlightpanels.Windows;
using NonVisuals.Interfaces;
using NonVisuals.Radios;
using NonVisuals.Saitek;

namespace DCSFlightpanels.Radios
{
    /// <summary>
    /// Interaction logic for RadioPanelPZ69UserControlEmulator.xaml
    /// </summary>
    public partial class RadioPanelPZ69UserControlEmulator : UserControlBase, IGamingPanelListener, IProfileHandlerListener, IGamingPanelUserControl
    {
        private readonly RadioPanelPZ69Emulator _radioPanelPZ69;
        private readonly TabItem _parentTabItem;
        private string _parentTabItemHeader;
        private IGlobalHandler _globalHandler;
        private bool _userControlLoaded;
        private bool _textBoxBills;
        private readonly List<Key> _allowedKeys = new List<Key>() { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.OemPeriod, Key.Delete, Key.Back, Key.Left, Key.Right, Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };

        public RadioPanelPZ69UserControlEmulator(HIDSkeleton hidSkeleton, TabItem parentTabItem, IGlobalHandler globalHandler)
        {
            InitializeComponent();
            _parentTabItem = parentTabItem;
            _parentTabItemHeader = _parentTabItem.Header.ToString();
            HideAllImages();

            _radioPanelPZ69 = new RadioPanelPZ69Emulator(hidSkeleton);
            _radioPanelPZ69.FrequencyKnobSensitivity = Settings.Default.RadioFrequencyKnobSensitivityEmulator;
            _radioPanelPZ69.Attach((IGamingPanelListener)this);
            globalHandler.Attach(_radioPanelPZ69);
            _globalHandler = globalHandler;

            //LoadConfiguration();
        }

        private void RadioPanelPZ69UserControlEmulator_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ComboBoxFreqKnobSensitivity.SelectedValue = Settings.Default.RadioFrequencyKnobSensitivityEmulator;
                SetTextBoxBills();
                SetContextMenuClickHandlers();
                _userControlLoaded = true;
                ShowGraphicConfiguration();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        public void BipPanelRegisterEvent(object sender, BipPanelRegisteredEventArgs e)
        {
            var now = DateTime.Now.Ticks;
            RemoveContextMenuClickHandlers();
            SetContextMenuClickHandlers();
        }

        public GamingPanel GetGamingPanel()
        {
            return _radioPanelPZ69;
        }

        public string GetName()
        {
            return GetType().Name;
        }

        public void UpdatesHasBeenMissed(object sender, DCSBIOSUpdatesMissedEventArgs e) { }

        public void SelectedAirframe(object sender, AirframeEventArgs e) { }

        public void UISwitchesChanged(object sender, SwitchesChangedEventArgs e)
        {
            try
            {
                if (e.GamingPanelEnum == GamingPanelEnum.PZ69RadioPanel && e.UniqueId.Equals(_radioPanelPZ69.InstanceId))
                {
                    NotifySwitchChanges(e.Switches);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        public void PanelSettingsReadFromFile(object sender, SettingsReadFromFileEventArgs e) { }

        public void SettingsCleared(object sender, PanelEventArgs e) { }

        public void LedLightChanged(object sender, LedLightChangeEventArgs e) { }

        public void PanelDataAvailable(object sender, PanelDataToDCSBIOSEventEventArgs e) { }

        public void DeviceAttached(object sender, PanelEventArgs e) { }

        public void DeviceDetached(object sender, PanelEventArgs e) { }

        public void SettingsApplied(object sender, PanelEventArgs e)
        {
            try
            {
                if (e.UniqueId.Equals(_radioPanelPZ69.InstanceId) && e.GamingPanelEnum == GamingPanelEnum.PZ69RadioPanel)
                {
                    Dispatcher?.BeginInvoke((Action)(ShowGraphicConfiguration));
                    Dispatcher?.BeginInvoke((Action)(() => TextBoxLogPZ69.Text = ""));
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        public void PanelSettingsChanged(object sender, PanelEventArgs e)
        {
            try
            {
                Dispatcher?.BeginInvoke((Action)(ShowGraphicConfiguration));
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void MouseDownFocusLogTextBox(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }



        private void SetTextBoxBills()
        {
            if (_textBoxBills || !Common.FindVisualChildren<PZ69TextBox>(this).Any())
            {
                return;
            }
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (textBox.Equals(TextBoxLogPZ69) || textBox.Name.EndsWith("Numbers"))
                {
                    continue;
                }
                textBox.Bill = new BillPZ69(textBox, GetPZ69Key(textBox));
            }
            _textBoxBills = true;
        }

        private void MenuContextEditTextBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }
                KeySequenceWindow keySequenceWindow;
                if (textBox.Bill.ContainsKeySequence())
                {
                    keySequenceWindow = new KeySequenceWindow(textBox.Text, textBox.Bill.GetKeySequence());
                }
                else
                {
                    keySequenceWindow = new KeySequenceWindow();
                }
                keySequenceWindow.ShowDialog();
                if (keySequenceWindow.DialogResult.HasValue && keySequenceWindow.DialogResult.Value)
                {
                    //Clicked OK
                    //If the user added only a single key stroke combo then let's not treat this as a sequence
                    if (!keySequenceWindow.IsDirty)
                    {
                        //User made no changes
                        return;
                    }
                    var sequenceList = keySequenceWindow.GetSequence;
                    textBox.ToolTip = null;
                    if (sequenceList.Count > 1)
                    {
                        var keyPress = new KeyPress("Key press sequence", sequenceList);
                        textBox.Bill.KeyPress = keyPress;
                        textBox.Bill.KeyPress.Information = keySequenceWindow.GetInformation;
                        if (!string.IsNullOrEmpty(keySequenceWindow.GetInformation))
                        {
                            textBox.Text = keySequenceWindow.GetInformation;
                        }
                        UpdateKeyBindingProfileSequencedKeyStrokesPZ69(textBox);
                    }
                    else
                    {
                        //If only one press was created treat it as a simple keypress
                        textBox.Bill.Clear();
                        var keyPress = new KeyPress(sequenceList[0].VirtualKeyCodesAsString, sequenceList[0].LengthOfKeyPress);
                        textBox.Bill.KeyPress = keyPress;
                        textBox.Bill.KeyPress.Information = keySequenceWindow.GetInformation;
                        textBox.Text = sequenceList[0].VirtualKeyCodesAsString;
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                }
                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }



        private void MenuContextEditBipTextBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }
                BIPLinkWindow bipLinkWindow;
                if (textBox.Bill.ContainsBIPLink())
                {
                    var bipLink = textBox.Bill.BIPLink;
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }
                else
                {
                    var bipLink = new BIPLinkPZ69();
                    bipLinkWindow = new BIPLinkWindow(bipLink);
                }
                bipLinkWindow.ShowDialog();
                if (bipLinkWindow.DialogResult.HasValue && bipLinkWindow.DialogResult == true && bipLinkWindow.IsDirty && bipLinkWindow.BIPLink != null && bipLinkWindow.BIPLink.BIPLights.Count > 0)
                {
                    textBox.Bill.BIPLink = (BIPLinkPZ69)bipLinkWindow.BIPLink;
                    UpdateBipLinkBindings(textBox);
                }

                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void UpdateKeyBindingProfileSequencedKeyStrokesPZ69(PZ69TextBox textBox)
        {
            try
            {
                var key = GetPZ69Key(textBox);
                _radioPanelPZ69.AddOrUpdateSequencedKeyBinding(textBox.Text, key.RadioPanelPZ69Key, textBox.Bill.GetKeySequence(), key.ButtonState);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void UpdateBipLinkBindings(PZ69TextBox textBox)
        {
            try
            {
                var key = GetPZ69Key(textBox);
                _radioPanelPZ69.AddOrUpdateBIPLinkKeyBinding(key.RadioPanelPZ69Key, textBox.Bill.BIPLink, key.ButtonState);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void UpdateKeyBindingProfileSimpleKeyStrokes(PZ69TextBox textBox)
        {
            try
            {
                KeyPressLength keyPressLength;
                if (!textBox.Bill.ContainsKeyPress() || textBox.Bill.KeyPress.KeySequence.Count == 0)
                {
                    keyPressLength = KeyPressLength.ThirtyTwoMilliSec;
                }
                else
                {
                    keyPressLength = textBox.Bill.KeyPress.GetLengthOfKeyPress();
                }
                var key = GetPZ69Key(textBox);
                _radioPanelPZ69.AddOrUpdateSingleKeyBinding(key.RadioPanelPZ69Key, textBox.Text, keyPressLength, key.ButtonState);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void UpdateOSCommandBindingsPZ55(PZ69TextBox textBox)
        {
            try
            {
                _radioPanelPZ69.AddOrUpdateOSCommandBinding(textBox.Bill.Knob.RadioPanelPZ69Key, textBox.Bill.OSCommandObject, textBox.Bill.Knob.ButtonState);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void UpdateDisplayValues(PZ69TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxUpperCom1ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperCOM1, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperCom1StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperCOM1, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperCom2ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperCOM2, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperCom2StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperCOM2, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperNav1ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperNAV1, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperNav1StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperNAV1, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperNav2ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperNAV2, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperNav2StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperNAV2, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperADFActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperADF, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperADFStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperADF, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperDMEActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperDME, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperDMEStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperDME, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }
                if (textBox.Equals(TextBoxUpperXPDRActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperXPDR, textBox.Text, RadioPanelPZ69Display.UpperActive);
                }
                if (textBox.Equals(TextBoxUpperXPDRStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.UpperXPDR, textBox.Text, RadioPanelPZ69Display.UpperStandby);
                }

                if (textBox.Equals(TextBoxLowerCom1ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerCOM1, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerCom1StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerCOM1, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerCom2ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerCOM2, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerCom2StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerCOM2, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerNav1ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerNAV1, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerNav1StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerNAV1, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerNav2ActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerNAV2, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerNav2StandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerNAV2, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerADFActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerADF, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerADFStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerADF, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerDMEActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerDME, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerDMEStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerDME, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
                if (textBox.Equals(TextBoxLowerXPDRActiveNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerXPDR, textBox.Text, RadioPanelPZ69Display.LowerActive);
                }
                if (textBox.Equals(TextBoxLowerXPDRStandbyNumbers))
                {
                    _radioPanelPZ69.AddOrUpdateDisplayValue(RadioPanelPZ69KnobsEmulator.LowerXPDR, textBox.Text, RadioPanelPZ69Display.LowerStandby);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void TextBoxContextMenuIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var contextMenu = (ContextMenu)sender;
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    foreach (MenuItem contextMenuItem in contextMenu.Items)
                    {
                        contextMenuItem.Visibility = Visibility.Collapsed;
                    }
                    return;
                    //throw new Exception("Failed to locate which textbox is focused.");
                }

                //Check new value, is menu visible?
                if (!(bool)e.NewValue)
                {
                    //Do not show if not visible
                    return;
                }

                if (!textBox.Bill.ContainsSingleKey())
                {
                    return;
                }
                var keyPressLength = textBox.Bill.KeyPress.GetLengthOfKeyPress();
                CheckContextMenuItems(keyPressLength, contextMenu);


            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ClearAll(bool clearAlsoProfile)
        {
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (textBox.Equals(TextBoxLogPZ69))
                {
                    continue;
                }
                textBox.Bill.Clear();
            }
            if (clearAlsoProfile)
            {
                _radioPanelPZ69.ClearSettings();
            }
        }

        private void ClearAllDisplayValues()
        {
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (textBox.Name.EndsWith("Numbers"))
                {
                    textBox.Text = "";
                }
            }
        }


        private void RemoveContextMenuClickHandlers()
        {
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ69))
                {
                    textBox.ContextMenu = null;
                    textBox.ContextMenuOpening -= TextBoxContextMenuOpening;
                }
            }
        }

        private void SetContextMenuClickHandlers()
        {
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ69) && !textBox.Name.EndsWith("Numbers"))
                {
                    var contectMenu = (ContextMenu)Resources["TextBoxContextMenuPZ69"];
                    if (!BipFactory.HasBips())
                    {
                        MenuItem bipMenuItem = null;
                        foreach (var item in contectMenu.Items)
                        {
                            if (((MenuItem)item).Name == "contextMenuItemEditBIP")
                            {
                                bipMenuItem = (MenuItem)item;
                                break;
                            }
                        }
                        if (bipMenuItem != null)
                        {
                            contectMenu.Items.Remove(bipMenuItem);
                        }
                    }
                    textBox.ContextMenu = contectMenu;
                    textBox.ContextMenuOpening += TextBoxContextMenuOpening;
                }
            }
        }

        private void TextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var textBox = (PZ69TextBox)sender;
                var contextMenu = textBox.ContextMenu;
                if (!(textBox.IsFocused && Equals(textBox.Background, Brushes.Yellow)))
                {
                    //UGLY Must use this to get around problems having different color for BIPLink and Right Clicks
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    return;
                }
                foreach (MenuItem item in contextMenu.Items)
                {
                    item.Visibility = Visibility.Collapsed;
                }
                if (textBox.Bill.ContainsKeySequence())
                {
                    // 2) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (textBox.Bill.IsEmpty())
                {
                    // 4) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else if (item.Name.Contains("EditOSCommand"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else if (item.Name.Contains("AddNullKey"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else if (textBox.Bill.ContainsSingleKey())
                {
                    // 5) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (!item.Name.Contains("EditSequence"))
                        {
                            if (item.Name.Contains("EditBIP"))
                            {
                                if (BipFactory.HasBips())
                                {
                                    item.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                item.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
                else if (textBox.Bill.ContainsBIPLink())
                {
                    // 3) 
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (BipFactory.HasBips() && item.Name.Contains("EditBIP"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                        if (item.Name.Contains("EditSequence"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (textBox.Bill.ContainsOSCommand())
                {
                    foreach (MenuItem item in contextMenu.Items)
                    {
                        if (item.Name.Contains("EditOSCommand"))
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private PZ69TextBox GetTextBoxInFocus()
        {
            foreach (var textBox in Common.FindVisualChildren<PZ69TextBox>(this))
            {
                if (!Equals(textBox, TextBoxLogPZ69) && textBox.IsFocused && Equals(textBox.Background, Brushes.Yellow))
                {
                    return textBox;
                }
            }
            return null;
        }

        private void TextBoxContextMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                SetKeyPressLength(textBox, (MenuItem)sender);
                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void TextBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBox = (PZ69TextBox)sender;

                if (e.ChangedButton == MouseButton.Left)
                {
                    if (textBox.Bill.ContainsKeySequence())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete the key sequence?", "Delete key sequence?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }
                        textBox.Bill.KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                    else if (textBox.Bill.ContainsSingleKey())
                    {
                        textBox.Bill.KeyPress.KeySequence.Clear();
                        textBox.Text = "";
                        UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
                    }
                    if (textBox.Bill.ContainsBIPLink())
                    {
                        //Check if this textbox contains sequence information. If so then prompt the user for deletion
                        if (MessageBox.Show("Do you want to delete BIP Links?", "Delete BIP Link?", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        {
                            return;
                        }
                        textBox.Bill.BIPLink.BIPLights.Clear();
                        textBox.Background = Brushes.White;
                        UpdateBipLinkBindings(textBox);
                    }
                }
                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ButtonClearAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Clear all settings for the Radio Panel?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    ClearAll(true);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                ((PZ69TextBox)sender).Background = Brushes.Yellow;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (PZ69TextBox)sender;
            if (textBox.Bill.ContainsBIPLink())
            {
                ((PZ69TextBox) sender).Background = Brushes.Bisque;
            }
            else
            {
                ((PZ69TextBox)sender).Background = Brushes.White;
            }
        }


        private void TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((PZ69TextBox)sender);

                //Check if this textbox contains sequence or DCS-BIOS information. If so then exit
                if (textBox.Bill.ContainsKeySequence())
                {
                    return;
                }
                var hashSetOfKeysPressed = new HashSet<string>();

                var keyCode = KeyInterop.VirtualKeyFromKey(e.RealKey());
                e.Handled = true;

                if (keyCode > 0)
                {
                    hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), keyCode));
                }
                var modifiers = CommonVK.GetPressedVirtualKeyCodesThatAreModifiers();
                foreach (var virtualKeyCode in modifiers)
                {
                    hashSetOfKeysPressed.Add(Enum.GetName(typeof(VirtualKeyCode), virtualKeyCode));
                }
                var result = "";
                foreach (var str in hashSetOfKeysPressed)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result = str + " + " + result;
                    }
                    else
                    {
                        result = str + " " + result;
                    }
                }

                result = Common.RemoveRControl(result);

                textBox.Text = result;
                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }
        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         */
        private void TextBoxPreviewKeyDownNumbers(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = ((PZ69TextBox)sender);

                if (textBox.Text.Contains("."))
                {
                    textBox.MaxLength = 6;
                }
                else
                {
                    textBox.MaxLength = 5;
                }
                if (!_allowedKeys.Contains(e.Key))
                {
                    //Only figures and persion allowed
                    e.Handled = true;
                    return;
                }
                if (textBox.Text.Contains(".") && e.Key == Key.OemPeriod)
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void TextBoxTextChangedNumbers(object sender, TextChangedEventArgs e)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void TextBoxNumbers_OnKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                var textBox = (PZ69TextBox)sender;
                UpdateDisplayValues(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void TextBoxMouseDoubleClickNumbers(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBox = (PZ69TextBox)sender;
                var radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperCOM1;
                var radioDisplay = RadioPanelPZ69Display.UpperActive;

                if (e.ChangedButton == MouseButton.Left)
                {
                    if (textBox.Name.Contains("Upper"))
                    {
                        if (textBox.Name.Contains("Com1"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperCOM1;
                        }
                        else if (textBox.Name.Contains("Com2"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperCOM2;
                        }
                        else if (textBox.Name.Contains("Nav1"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperNAV1;
                        }
                        else if (textBox.Name.Contains("Nav2"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperNAV2;
                        }
                        else if (textBox.Name.Contains("ADF"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperADF;
                        }
                        else if (textBox.Name.Contains("DME"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperDME;
                        }
                        else if (textBox.Name.Contains("XPDR"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.UpperXPDR;
                        }

                        else if (textBox.Name.Contains("Active"))
                        {
                            radioDisplay = RadioPanelPZ69Display.UpperActive;
                        }

                        else if (textBox.Name.Contains("Standby"))
                        {
                            radioDisplay = RadioPanelPZ69Display.UpperStandby;
                        }
                    }
                    if (textBox.Name.Contains("Lower"))
                    {
                        if (textBox.Name.Contains("Com1"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerCOM1;
                        }
                        else if (textBox.Name.Contains("Com2"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerCOM2;
                        }
                        else if (textBox.Name.Contains("Nav1"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerNAV1;
                        }
                        else if (textBox.Name.Contains("Nav2"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerNAV2;
                        }
                        else if (textBox.Name.Contains("ADF"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerADF;
                        }
                        else if (textBox.Name.Contains("DME"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerDME;
                        }
                        else if (textBox.Name.Contains("XPDR"))
                        {
                            radioPanelKnob = RadioPanelPZ69KnobsEmulator.LowerXPDR;
                        }

                        else if (textBox.Name.Contains("Active"))
                        {
                            radioDisplay = RadioPanelPZ69Display.LowerActive;
                        }

                        if (textBox.Name.Contains("Standby"))
                        {
                            radioDisplay = RadioPanelPZ69Display.LowerStandby;
                        }
                    }

                    _radioPanelPZ69.AddOrUpdateDisplayValue(radioPanelKnob, "-1", radioDisplay);
                    ClearAllDisplayValues();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         * ------------------------------------------------------------------------------------------------------------------------------------------------------------
         */

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //MAKE SURE THE TAG IS SET BEFORE SETTING TEXT! OTHERWISE THIS DOESN'T FIRE
                var textBox = (PZ69TextBox)sender;
                if (textBox.Bill.ContainsKeySequence())
                {
                    textBox.FontStyle = FontStyles.Oblique;
                }
                else
                {
                    textBox.FontStyle = FontStyles.Normal;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }
        
        private void NotifySwitchChanges(HashSet<object> switches)
        {
            try
            {
                //Set focus to this so that virtual keypresses won't affect settings
                Dispatcher?.BeginInvoke((Action)(() => TextBoxLogPZ69.Focus()));
                foreach (var radioPanelKey in switches)
                {
                    var key = (RadioPanelPZ69KnobEmulator)radioPanelKey;

                    if (_radioPanelPZ69.ForwardPanelEvent)
                    {
                        if (!string.IsNullOrEmpty(_radioPanelPZ69.GetKeyPressForLoggingPurposes(key)))
                        {
                            Dispatcher?.BeginInvoke(
                                (Action)
                                (() =>
                                 TextBoxLogPZ69.Text =
                                 TextBoxLogPZ69.Text.Insert(0, _radioPanelPZ69.GetKeyPressForLoggingPurposes(key) + "\n")));
                        }
                    }
                    else
                    {
                        Dispatcher?.BeginInvoke(
                            (Action)
                            (() =>
                             TextBoxLogPZ69.Text =
                             TextBoxLogPZ69.Text = TextBoxLogPZ69.Text.Insert(0, "No action taken, panel events Disabled.\n")));
                    }
                }
                SetGraphicsState(switches);
            }
            catch (Exception ex)
            {
                Dispatcher?.BeginInvoke(
                    (Action)
                    (() =>
                     TextBoxLogPZ69.Text = TextBoxLogPZ69.Text.Insert(0, "0x16" + ex.Message + ".\n")));
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ShowGraphicConfiguration()
        {
            try
            {
                if (!_userControlLoaded || !_textBoxBills)
                {
                    return;
                }
                foreach (var displayValue in _radioPanelPZ69.DisplayValueHashSet)
                {
                    if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperCOM1)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperCom1ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperCom1StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperCOM2)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperCom2ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperCom2StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperNAV1)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperNav1ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperNav1StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperNAV2)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperNav2ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperNav2StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperADF)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperADFActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperADFStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperDME)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperDMEActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperDMEStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.UpperXPDR)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperActive)
                        {
                            TextBoxUpperXPDRActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.UpperStandby)
                        {
                            TextBoxUpperXPDRStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerCOM1)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerCom1ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerCom1StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerCOM2)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerCom2ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerCom2StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerNAV1)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerNav1ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerNav1StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerNAV2)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerNav2ActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerNav2StandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerADF)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerADFActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerADFStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerDME)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerDMEActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerDMEStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                    else if (displayValue.RadioPanelPZ69Knob == RadioPanelPZ69KnobsEmulator.LowerXPDR)
                    {
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerActive)
                        {
                            TextBoxLowerXPDRActiveNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                        if (displayValue.RadioPanelDisplay == RadioPanelPZ69Display.LowerStandby)
                        {
                            TextBoxLowerXPDRStandbyNumbers.Text = displayValue.Value.ToString(Common.GetPZ69FullDisplayNumberFormat());
                        }
                    }
                }

                foreach (var keyBinding in _radioPanelPZ69.KeyBindingsHashSet)
                {
                    var textBox = GetTextBox(keyBinding.RadioPanelPZ69Key, keyBinding.WhenTurnedOn);
                    if (keyBinding.OSKeyPress != null)
                    {
                        textBox.Bill.KeyPress = keyBinding.OSKeyPress;
                    }
                }

                foreach (var osCommand in _radioPanelPZ69.OSCommandHashSet)
                {
                    var textBox = GetTextBox(osCommand.RadioPanelPZ69Key, osCommand.WhenTurnedOn);
                    if (osCommand.OSCommandObject != null)
                    {
                        textBox.Bill.OSCommandObject = osCommand.OSCommandObject;
                    }
                }

                foreach (var bipLinkPZ69 in _radioPanelPZ69.BipLinkHashSet)
                {
                    var textBox = GetTextBox(bipLinkPZ69.RadioPanelPZ69Knob, bipLinkPZ69.WhenTurnedOn);
                    textBox.Bill.BIPLink = bipLinkPZ69;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void SetGraphicsState(HashSet<object> knobs)
        {
            try
            {
                foreach (var radioKnobO in knobs)
                {
                    var radioKnob = (RadioPanelPZ69KnobEmulator)radioKnobO;
                    switch (radioKnob.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsEmulator.UpperCOM1:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperCOM2:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperNAV1:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperNAV2:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperADF:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperDME:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperXPDR:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerCOM1:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerCOM2:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerNAV1:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerNAV2:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerADF:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerDME:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerXPDR:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.UpperFreqSwitch:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsEmulator.LowerFreqSwitch:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void HideAllImages()
        {
            TopLeftCom1.Visibility = Visibility.Collapsed;
            TopLeftCom2.Visibility = Visibility.Collapsed;
            TopLeftNav1.Visibility = Visibility.Collapsed;
            TopLeftNav2.Visibility = Visibility.Collapsed;
            TopLeftADF.Visibility = Visibility.Collapsed;
            TopLeftDME.Visibility = Visibility.Collapsed;
            TopLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLeftCom1.Visibility = Visibility.Collapsed;
            LowerLeftCom2.Visibility = Visibility.Collapsed;
            LowerLeftNav1.Visibility = Visibility.Collapsed;
            LowerLeftNav2.Visibility = Visibility.Collapsed;
            LowerLeftADF.Visibility = Visibility.Collapsed;
            LowerLeftDME.Visibility = Visibility.Collapsed;
            LowerLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperRightSwitch.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            LowerRightSwitch.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
        }

        private void ButtonGetId_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_radioPanelPZ69 != null)
                {
                    TextBoxLogPZ69.Text = "";
                    TextBoxLogPZ69.Text = _radioPanelPZ69.InstanceId;
                    Clipboard.SetText(_radioPanelPZ69.InstanceId);
                    MessageBox.Show("The Instance Id for the panel has been copied to the Clipboard.");
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ComboBoxFreqKnobSensitivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_userControlLoaded)
                {
                    Settings.Default.RadioFrequencyKnobSensitivityEmulator = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    _radioPanelPZ69.FrequencyKnobSensitivity = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private RadioPanelPZ69KeyOnOff GetPZ69Key(PZ69TextBox textBox)
        {
            try
            {
                if (textBox.Equals(TextBoxUpperCom1))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperCOM1, true);
                }
                if (textBox.Equals(TextBoxUpperCom2))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperCOM2, true);
                }
                if (textBox.Equals(TextBoxUpperNav1))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperNAV1, true);
                }
                if (textBox.Equals(TextBoxUpperNav2))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperNAV2, true);
                }
                if (textBox.Equals(TextBoxUpperADF))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperADF, true);
                }
                if (textBox.Equals(TextBoxUpperDME))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperDME, true);
                }
                if (textBox.Equals(TextBoxUpperXPDR))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperXPDR, true);
                }
                if (textBox.Equals(TextBoxUpperLargePlus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxUpperLargeMinus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxUpperSmallPlus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxUpperSmallMinus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxUpperActStbyOn))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, true);
                }
                if (textBox.Equals(TextBoxUpperActStbyOff))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.UpperFreqSwitch, false);
                }
                if (textBox.Equals(TextBoxLowerCom1))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerCOM1, true);
                }
                if (textBox.Equals(TextBoxLowerCom2))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerCOM2, true);
                }
                if (textBox.Equals(TextBoxLowerNav1))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerNAV1, true);
                }
                if (textBox.Equals(TextBoxLowerNav2))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerNAV2, true);
                }
                if (textBox.Equals(TextBoxLowerADF))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerADF, true);
                }
                if (textBox.Equals(TextBoxLowerDME))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerDME, true);
                }
                if (textBox.Equals(TextBoxLowerXPDR))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerXPDR, true);
                }
                if (textBox.Equals(TextBoxLowerLargePlus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxLowerLargeMinus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxLowerSmallPlus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc, true);
                }
                if (textBox.Equals(TextBoxLowerSmallMinus))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec, true);
                }
                if (textBox.Equals(TextBoxLowerActStbyOn))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, true);
                }
                if (textBox.Equals(TextBoxLowerActStbyOff))
                {
                    return new RadioPanelPZ69KeyOnOff(RadioPanelPZ69KnobsEmulator.LowerFreqSwitch, false);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
            throw new Exception("Failed to find Radiopanel knob for TextBox " + textBox.Name);
        }

        private PZ69TextBox GetTextBox(RadioPanelPZ69KnobsEmulator knob, bool whenTurnedOn)
        {
            try
            {
                if (knob == RadioPanelPZ69KnobsEmulator.UpperCOM1 && whenTurnedOn)
                {
                    return TextBoxUpperCom1;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperCOM2 && whenTurnedOn)
                {
                    return TextBoxUpperCom2;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperNAV1 && whenTurnedOn)
                {
                    return TextBoxUpperNav1;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperNAV2 && whenTurnedOn)
                {
                    return TextBoxUpperNav2;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperADF && whenTurnedOn)
                {
                    return TextBoxUpperADF;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperDME && whenTurnedOn)
                {
                    return TextBoxUpperDME;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperXPDR && whenTurnedOn)
                {
                    return TextBoxUpperXPDR;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelInc && whenTurnedOn)
                {
                    return TextBoxUpperLargePlus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperLargeFreqWheelDec && whenTurnedOn)
                {
                    return TextBoxUpperLargeMinus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelInc && whenTurnedOn)
                {
                    return TextBoxUpperSmallPlus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperSmallFreqWheelDec && whenTurnedOn)
                {
                    return TextBoxUpperSmallMinus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperFreqSwitch && whenTurnedOn)
                {
                    return TextBoxUpperActStbyOn;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.UpperFreqSwitch && !whenTurnedOn)
                {
                    return TextBoxUpperActStbyOff;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerCOM1 && whenTurnedOn)
                {
                    return TextBoxLowerCom1;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerCOM2 && whenTurnedOn)
                {
                    return TextBoxLowerCom2;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerNAV1 && whenTurnedOn)
                {
                    return TextBoxLowerNav1;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerNAV2 && whenTurnedOn)
                {
                    return TextBoxLowerNav2;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerADF && whenTurnedOn)
                {
                    return TextBoxLowerADF;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerDME && whenTurnedOn)
                {
                    return TextBoxLowerDME;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerXPDR && whenTurnedOn)
                {
                    return TextBoxLowerXPDR;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelInc && whenTurnedOn)
                {
                    return TextBoxLowerLargePlus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerLargeFreqWheelDec && whenTurnedOn)
                {
                    return TextBoxLowerLargeMinus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelInc && whenTurnedOn)
                {
                    return TextBoxLowerSmallPlus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerSmallFreqWheelDec && whenTurnedOn)
                {
                    return TextBoxLowerSmallMinus;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerFreqSwitch && whenTurnedOn)
                {
                    return TextBoxLowerActStbyOn;
                }
                if (knob == RadioPanelPZ69KnobsEmulator.LowerFreqSwitch && !whenTurnedOn)
                {
                    return TextBoxLowerActStbyOff;
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
            throw new Exception("Failed to find TextBox for Radiopanel knob " + knob);
        }
        
        private void MenuItemAddNullKey_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }

                textBox.Bill.Clear();
                var vkNull = Enum.GetName(typeof(VirtualKeyCode), VirtualKeyCode.VK_NULL);
                if (string.IsNullOrEmpty(vkNull))
                {
                    return;
                }
                var keyPress = new KeyPress(vkNull, KeyPressLength.ThirtyTwoMilliSec);
                textBox.Bill.KeyPress = keyPress;
                textBox.Bill.KeyPress.Information = "VK_NULL";
                textBox.Text = vkNull;
                UpdateKeyBindingProfileSimpleKeyStrokes(textBox);
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void MenuContextEditOSCommandTextBoxClick_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = GetTextBoxInFocus();
                if (textBox == null)
                {
                    throw new Exception("Failed to locate which textbox is focused.");
                }
                OSCommandWindow osCommandWindow;
                if (textBox.Bill.ContainsOSCommand())
                {
                    osCommandWindow = new OSCommandWindow(textBox.Bill.OSCommandObject);
                }
                else
                {
                    osCommandWindow = new OSCommandWindow();
                }
                osCommandWindow.ShowDialog();
                if (osCommandWindow.DialogResult.HasValue && osCommandWindow.DialogResult.Value)
                {
                    //Clicked OK
                    if (!osCommandWindow.IsDirty)
                    {
                        //User made no changes
                        return;
                    }
                    var osCommand = osCommandWindow.OSCommandObject;
                    textBox.Bill.OSCommandObject = osCommand;
                    textBox.Text = osCommand.Name;
                    UpdateOSCommandBindingsPZ55(textBox);
                }
                TextBoxLogPZ69.Focus();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }


        private void CheckContextMenuItems(KeyPressLength keyPressLength, ContextMenu contextMenu)
        {
            try
            {
                foreach (MenuItem item in contextMenu.Items)
                {
                    item.IsChecked = false;
                }

                foreach (MenuItem item in contextMenu.Items)
                {
                    if (item.Name == "contextMenuItemKeepPressed" && keyPressLength == KeyPressLength.Indefinite)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemThirtyTwoMilliSec" && keyPressLength == KeyPressLength.ThirtyTwoMilliSec)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFiftyMilliSec" && keyPressLength == KeyPressLength.FiftyMilliSec)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemHalfSecond" && keyPressLength == KeyPressLength.HalfSecond)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSecond" && keyPressLength == KeyPressLength.Second)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSecondAndHalf" && keyPressLength == KeyPressLength.SecondAndHalf)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTwoSeconds" && keyPressLength == KeyPressLength.TwoSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemThreeSeconds" && keyPressLength == KeyPressLength.ThreeSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFourSeconds" && keyPressLength == KeyPressLength.FourSeconds)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFiveSecs" && keyPressLength == KeyPressLength.FiveSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFifteenSecs" && keyPressLength == KeyPressLength.FifteenSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTenSecs" && keyPressLength == KeyPressLength.TenSecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemTwentySecs" && keyPressLength == KeyPressLength.TwentySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemThirtySecs" && keyPressLength == KeyPressLength.ThirtySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemFortySecs" && keyPressLength == KeyPressLength.FortySecs)
                    {
                        item.IsChecked = true;
                    }
                    else if (item.Name == "contextMenuItemSixtySecs" && keyPressLength == KeyPressLength.SixtySecs)
                    {
                        item.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }

        private void SetKeyPressLength(PZ69TextBox textBox, MenuItem contextMenuItem)
        {
            try
            {
                if (contextMenuItem.Name == "contextMenuItemKeepPressed")
                {
                    var message = "Remember to set a command for the opposing action!\n\n" +
                                  "For example if you set Keep Pressed for the \"On\" position for a button you need to set a command for \"Off\" position.\n" +
                                  "This way the continuous Keep Pressed will be canceled.\n" +
                                  "If you do not want a key press to cancel the continuous key press you can add a \"VK_NULL\" key.\n" +
                                  "\"VK_NULL\'s\" sole purpose is to cancel a continuous key press.";
                    var infoDialog = new InformationTextBlockWindow(message);
                    infoDialog.Height = 250;
                    infoDialog.ShowDialog();
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.Indefinite);
                }
                else if (contextMenuItem.Name == "contextMenuItemThirtyTwoMilliSec")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.ThirtyTwoMilliSec);
                }
                else if (contextMenuItem.Name == "contextMenuItemFiftyMilliSec")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.FiftyMilliSec);
                }
                else if (contextMenuItem.Name == "contextMenuItemHalfSecond")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.HalfSecond);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecond")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.Second);
                }
                else if (contextMenuItem.Name == "contextMenuItemSecondAndHalf")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.SecondAndHalf);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwoSeconds")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.TwoSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemThreeSeconds")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.ThreeSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFourSeconds")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.FourSeconds);
                }
                else if (contextMenuItem.Name == "contextMenuItemFiveSecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.FiveSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTenSecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.TenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFifteenSecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.FifteenSecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemTwentySecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.TwentySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemThirtySecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.ThirtySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemFortySecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.FortySecs);
                }
                else if (contextMenuItem.Name == "contextMenuItemSixtySecs")
                {
                    textBox.Bill.KeyPress.SetLengthOfKeyPress(KeyPressLength.SixtySecs);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }
    }
}
