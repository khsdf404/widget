   M S B A M L   `   `   `  ���� A  =widget, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null5clr-namespace:widget.ViewModelswidget.ViewModels  clr-namespace:widgetwidget      widget.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     .vmclr-namespace:widget.ViewModels     "lclr-namespace:widget     -     � Title$  widget��$��90��$	��600�����	#22777777 � 
ResizeMode$ NoResize=� � AllowsTransparency$
 True�� � WindowStyle$
 None=� � ShowInTaskbar$ False�� � DataContext +  %widget.ViewModels.MainWindowViewModel   � 	Resources    icon_btn �� Property �� Value  NoMouseOverButtonTemplate  border_right  	underline  clock_lines��%�  &	        7  &	 h     7  &	 �     7  &	     7  �� .���  � ��$ 0q���  � �� �#202020��  �@J�Opacity$ 1Q���  � ��8   �� .���  � ��$ 3,3,3,3q���  � �� �#111��  � ��8   �� .���  � ��$ 0q���  � �� �#454545��  � ��8   �� .���  � ��$	 1px����  � ��$ 0q���  � �� �#fff��  �@J�Opacity$
 0.65Q���  � ��8   .���� 	 �� Padding$	 1,0,1,0q�._�� 
 � RowDefinitions
 �� $��9* ��� $
��1.5* � � ColumnDefinitions �� $��1* ��� $	��1px ��� $��1* ��� $	��1px ��� $
��1.5* ��� $	��1px ��� $��1* ��� $	��1px ��� $��1* �.+���  �� CornerRadius$ 4��#��[  $��0��$��0��$��2���� -   #��[  $��4,0,0,0q�$��0��$��0��$��2��.���� -    ��Name$ 	wifi_icon��$��wifi_ico.png��$
��58px�� J� Opacity$
 0.25Q�$��
-4,-14,0,0q� �� BitmapScalingMode$ HighQuality=��� -   #��[  $��2��$��0��$��2��.���� -   $ bt_icon��$��
bt_ico.png��$
��60px��$
 0.25Q�$��	0,-12,0,0q�$ HighQuality=��� ���	#44444466#��[  $��4��$��0��$��2��.��� 
 �� $��1* ��� $��6* ��� $��2* ��� $��1* � �� $��1* �.+��� #��[ $��0���� -    ��Name$ clock��$��1��$��12:12�����#fff$��0,-3,0,0q�$
��42px(��� -   $
 date��$��2��$��
Monday, 24�����#fff$��0,-6,0,7q�$
��10px(�$��Center=��� #��[ $��3���� -   #��[  $��6��$��0��$��2��.���� -	   $ 
music_icon��$��headphones.png��$
��54px��$
 0.30Q�$��	3,-14,0,0q�$ HighQuality=��� $ 4��#��[  $��8��$��0��$��2���� -
   #��[  $��0,0,4,0q�$��8��$��0��$��2��.���� -   $ RecBin��$��recyclebin.png��$
��58px��$��
0,-14,-4,0q�$ HighQuality=��� -    ��Name$ WifiLine��#��[ $ 2��$��0��$��1���� -   $ BtLine��#��[ $ 2��$��2��$��1���� -   $ 	MusicLine��#��[ $ 2��$��6��$��1���� -   $ 
RecBinLine��#��[ ���#fff$ 2��$��8��$��1���� #��[ $��1��$��0���� #��[ $��3��$��0���� #��[ $��5��$��0���� #��[ $��7��$��0���� #��[ $��1��$��1���� #��[ $��3��$��1���� #��[ $��5��$��1���� #��[ $��7��$��1��