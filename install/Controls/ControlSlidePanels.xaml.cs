﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using installEAS.Helpers;
using static installEAS.MainWindow;
using static installEAS.Helpers.Animate;

namespace installEAS.Controls;

public partial class UIControlSlidePanels
{
    public static void CreateSlidePanelsInstance()
    {
        var variablesInstance = CreateSlidePanelsInstance;
        Console.WriteLine(variablesInstance.Method);
    }

    public UIControlSlidePanels()
    {
        InitializeComponent();
        PanelTopMain.IsEnabled = false;
        PanelTopAdd.IsEnabled  = false;
    }


    private void btnMainMenu1_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 300);
        }, DispatcherPriority.Send);
    }

    private void BtnMainMenu2_OnClick(object sender, RoutedEventArgs e)
    {
    }


    private void btnMainMenu3_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 300);
        }, DispatcherPriority.Background);
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopAdd, 300);
        }, DispatcherPriority.Send);
    }

    private void BtnMainMenu4_OnClick(object sender, RoutedEventArgs e)
    {
        CloseMain();
    }

    [STAThread]
    private new void MouseLeave(object sender, MouseEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var element = (FrameworkElement)sender;
            ColorAnimation(new InClassName(element, controlFrom, controlTo, 150));
        }, DispatcherPriority.Background);
    }

    private void BtnMenuAdd0_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopAdd, 300);
        }, DispatcherPriority.Background);
        Dispatcher.InvokeOrExecute(() =>
        {
            var unused = AnimateFrameworkElement(MenuMain.PanelTopMain, 300);
        }, DispatcherPriority.Send);
    }
}