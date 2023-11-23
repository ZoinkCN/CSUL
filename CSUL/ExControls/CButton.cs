﻿using System;
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

namespace CSUL.ExControls
{
    /// <summary>
    /// 自定义按钮控件
    /// </summary>
    public class CButton : Button
    {
        static CButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CButton), new FrameworkPropertyMetadata(typeof(CButton)));
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(CButton), new PropertyMetadata(null));
        public ImageSource Icon
        {   //图标属性
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ButtonTypeProperty =
            DependencyProperty.Register("ButtonType", typeof(CButtonType), typeof(CButton), new PropertyMetadata(CButtonType.Icon));
        public CButtonType ButtonType
        {   //按钮类型属性
            get => (CButtonType)GetValue(ButtonTypeProperty);
            set => SetValue(ButtonTypeProperty, value);
        }

        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register("PathData", typeof(Geometry), typeof(CButton), new PropertyMetadata(null));
        public Geometry PathData
        {   //Path类型下的绘制内容
            get => (Geometry)GetValue(PathDataProperty);
            set => SetValue(PathDataProperty, value);
        }

        //public static readonly DependencyProperty RotateProperty =
        //    DependencyProperty.Register("Rotate", typeof(bool), typeof(CButton), new PropertyMetadata(true));
        //public bool Rotate
        //{   //是否旋转
        //    get => (bool)GetValue(RotateProperty); 
        //    set => SetValue(RotateProperty, value);
        //}
    }
}
