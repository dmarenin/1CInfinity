object Form1: TForm1
  Left = 433
  Top = 276
  Width = 217
  Height = 177
  Caption = 'Form1'
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  OldCreateOrder = False
  Position = poDesktopCenter
  PixelsPerInch = 96
  TextHeight = 13
  object Panel1: TPanel
    Left = 0
    Top = 0
    Width = 209
    Height = 150
    Align = alClient
    BevelOuter = bvNone
    TabOrder = 0
    object Button1: TButton
      Left = 8
      Top = 16
      Width = 75
      Height = 25
      Caption = 'Load Core'
      TabOrder = 0
      OnClick = Button1Click
    end
    object Button2: TButton
      Left = 104
      Top = 16
      Width = 75
      Height = 25
      Caption = 'Unload Core'
      TabOrder = 1
      OnClick = Button2Click
    end
    object Button5: TButton
      Left = 8
      Top = 59
      Width = 75
      Height = 25
      Caption = 'Logon'
      TabOrder = 2
      OnClick = Button5Click
    end
    object Button6: TButton
      Left = 104
      Top = 59
      Width = 75
      Height = 25
      Caption = 'Close'
      TabOrder = 3
      OnClick = Button6Click
    end
    object Button3: TButton
      Left = 8
      Top = 99
      Width = 75
      Height = 25
      Caption = 'Extensions'
      TabOrder = 4
      OnClick = Button3Click
    end
  end
end
