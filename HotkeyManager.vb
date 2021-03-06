'Author: Arman Ghazanchyan
'Created date: 03/10/2008
'Last updated: 10/30/2008

Imports System.ComponentModel
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Collections.ObjectModel

#Region " Structures "

<DebuggerNonUserCode()> _
<DebuggerDisplay("Id = {Id}, Name = {Name}"), CLSCompliant(True)> _
Public Structure Hotkey
    Private _data As Keys
    Private _id As Integer
    ''' <summary>
    ''' Represents a Hotkey that has Hotkey.Id set to zero and Hotkey.Data set to System.Windows.Forms.Keys.None.
    ''' </summary>
    Public Shared ReadOnly Empty As Hotkey

    ''' <param name="id">A unique hotkey id that is in the range 0x0000 through 0xBFFF.</param>
    ''' <param name="data">The Hotkey data. This parameter can contain one or combination of 
    ''' Keys.Ctrl, Keys.Alt or Keys.Shift fodifier keys and any key, combined with OR.</param>
    Sub New(ByVal id As Integer, ByVal data As Keys)
        If (id >= &H0 AndAlso id <= &HBFFF) Then
            Me._id = id
            Me._data = data
        Else
            Throw New ArgumentOutOfRangeException("id", id, "The hotkey's id value must be in the range 0x0000 through 0xBFFF.")
        End If
    End Sub

    ''' <summary>
    ''' Gets a Boolean indicating if the ALT key is present.
    ''' </summary>
    Public ReadOnly Property Alt() As Boolean
        Get
            Return (Me._data And Keys.Alt) = Keys.Alt
        End Get
    End Property

    ''' <summary>
    ''' Gets a Boolean indicating if the CTRL key is present.
    ''' </summary>
    Public ReadOnly Property Control() As Boolean
        Get
            Return (Me._data And Keys.Control) = Keys.Control
        End Get
    End Property

    ''' <summary>
    ''' Gets a Boolean indicating if the SHIFT key is present.
    ''' </summary>
    Public ReadOnly Property Shift() As Boolean
        Get
            Return (Me._data And Keys.Shift) = Keys.Shift
        End Get
    End Property

    ''' <summary>
    ''' Gets the modifiers of the hotkey.
    ''' </summary>
    Public ReadOnly Property Modifiers() As Keys
        Get
            Return Me._data And Keys.Modifiers
        End Get
    End Property

    ''' <summary>
    ''' Gets the key of the hotkey.
    ''' </summary>
    Public ReadOnly Property Key() As Keys
        Get
            Return Me._data And Not Keys.Modifiers
        End Get
    End Property

    ''' <summary>
    ''' Gets or sets the hotkey data. Hotkey data can contain one or combination of 
    ''' Keys.Ctrl, Keys.Alt or Keys.Shift fodifier keys and any key, combined with OR.
    ''' </summary>
    Public Property Data() As Keys
        Get
            Return Me._data
        End Get
        Set(ByVal value As Keys)
            Me._data = value
        End Set
    End Property

    ''' <summary>
    ''' Gets the hotkey id.
    ''' </summary>
    Public ReadOnly Property Id() As Integer
        Get
            Return Me._id
        End Get
    End Property

    ''' <summary>
    ''' Gets the hotkey name.
    ''' </summary>
    Public ReadOnly Property Name() As String
        Get
            Return Me.ToString
        End Get
    End Property

    ''' <summary>
    ''' Determines whether the specified System.Object is equal to the current System.Object.
    ''' </summary>
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If TypeOf obj Is Hotkey Then
            Dim hk As Hotkey = DirectCast(obj, Hotkey)
            Return Me._id = hk._id AndAlso Me._data = hk._data
        End If
        Return False
    End Function

    Public Shared Operator =(ByVal obj1 As Hotkey, ByVal obj2 As Hotkey) As Boolean
        Return obj1.Equals(obj2)
    End Operator

    Public Shared Operator <>(ByVal obj1 As Hotkey, ByVal obj2 As Hotkey) As Boolean
        Return Not obj1.Equals(obj2)
    End Operator

    ''' <summary>
    ''' Serves as a hash function for a particular type. Hotkey.GetHashCode is 
    ''' suitable for use in hashing algorithms and data structures like a hash table.
    ''' </summary>
    Public Overrides Function GetHashCode() As Integer
        Return Me._id Xor ((Me._data << 7) Or (Me._data >> 25))
    End Function

    ''' <summary>
    ''' Returns a System.String that represents the current Hotkey.
    ''' </summary>
    Public Overrides Function ToString() As String
        Dim str As String = String.Empty
        If Me.Control Then
            str = "Ctrl+"
        End If
        If Me.Alt Then
            str &= "Alt+"
        End If
        If Me.Shift Then
            str &= "Shift+"
        End If
        str &= Me.Key.ToString
        Return str
    End Function

    ''' <summary>
    ''' Returns a System.String that represents the specified Hotkey.
    ''' </summary>
    ''' <param name="data">A Hotkey data. This parameter can contain one or combination of 
    ''' Keys.Ctrl, Keys.Alt or Keys.Shift fodifier keys and any key, combined with OR.</param>
    Public Overloads Shared Function ToString(ByVal data As Keys) As String
        Return New Hotkey(0, data).ToString
    End Function

End Structure

#End Region

''' <summary>
''' Registers or unregisters application hotkeys.
''' </summary>
<CLSCompliant(True), DebuggerNonUserCode()> _
Public Class HotkeyManager
    Implements IDisposable

#Region " Enumarations "

    Private Enum Modifier As Integer
        None = &H0
        Alt = &H1
        Control = &H2
        Shift = &H4
    End Enum

#End Region

#Region " Event Handlers "

    ''' <summary>
    ''' Occurs when a registered hotkey by the HotkeyManager is pressed.
    ''' </summary>
    <Description("Occurs when a registered hotkey by the HotkeyManager is pressed.")> _
    Event HotkeyPressed As EventHandler(Of HotkeyEventArgs)

#End Region

    Private _hotkeys As New Dictionary(Of Integer, Keys)
    Private _hotkeyProc As New HotkeyPorc(Me)

#Region " Properties "

    ''' <summary>
    ''' Gets a collection of hot keys that have been registered by the HotkeyManager.
    ''' </summary>
    Public ReadOnly Property Hotkeys() As HotkeyCollection
        Get
            Dim hCollection As New Collection(Of Hotkey)
            For Each key As Integer In Me._hotkeys.Keys
                hCollection.Add(New Hotkey(key, Me._hotkeys.Item(key)))
            Next
            Return New HotkeyCollection(hCollection)
        End Get
    End Property

    ''' <summary>
    ''' Gets the handle to the window associated with the HotkeyManager.
    ''' </summary>
    Public ReadOnly Property Handle() As IntPtr
        Get
            Return Me._hotkeyProc.Handle
        End Get
    End Property

#End Region

#Region " Methods "

    ''' <param name="window">
    ''' A valid window (Form) within the project associated with the HotkeyManager.
    ''' </param>
    Sub New(ByVal window As Form)
        If window IsNot Nothing Then
            Me._hotkeyProc.AssignHandle(window.Handle)
        Else
            Throw New ArgumentNullException("window", "The window (Form) cannot be Null (Nothing in VB).")
        End If
    End Sub

    ''' <summary>
    ''' Registers an application hotkey.
    ''' </summary>
    ''' <param name="hotkeyId">A unique hotkey id that is in the range 0x0000 through 0xBFFF.</param>
    ''' <param name="hotkeyData">A Hotkey data. This parameter can contain one or combination of 
    ''' Keys.Ctrl, Keys.Alt or Keys.Shift fodifier keys and any key, combined with OR.</param>
    ''' <param name="throwException">Specifies whether an exception should be thrown after the method fails.</param>
    Public Overloads Function RegisterHotkey(ByVal hotkeyId As Integer, ByVal hotkeyData As Keys, ByVal throwException As Boolean) As Boolean
        Dim hk As New Hotkey(hotkeyId, hotkeyData)
        Me.RegisterHotKey(hk, throwException)
    End Function

    ''' <summary>
    ''' Registers an application hotkey.
    ''' </summary>
    ''' <param name="hk">A HotkeyManager.Hotkey to registerd.</param>
    ''' <param name="throwException">Specifies whether an exception should be thrown after the method fails.</param>
    Public Overloads Function RegisterHotkey(ByVal hk As Hotkey, ByVal throwException As Boolean) As Boolean
        Dim ex As HotkeyException
        If hk.Key <> Keys.None Then
            If Not Me._hotkeys.ContainsKey(hk.Id) Then
                If Not NativeMethods.RegisterHotKey( _
                Me._hotkeyProc.Handle, hk.Id, HotkeyManager.ConvertTo(hk.Modifiers), hk.Key) Then
                    Dim eCode As Integer = Marshal.GetLastWin32Error
                    ex = New HotkeyException(New Win32Exception(eCode).Message)
                Else
                    Me._hotkeys.Add(hk.Id, hk.Data)
                    Return True
                End If
            Else
                ex = New HotkeyException("A hot key with the same id is already registered.")
            End If
        Else
            ex = New HotkeyException("The hotkey must contain a valid key.")
        End If
        If throwException Then
            Throw ex
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Unregisters an application hotkey that was previously registered.
    ''' </summary>
    ''' <param name="hotkeyId">A hotkey id. 
    ''' If the function succeeds, the return value is the unregistered hotkey.</param>
    ''' <param name="throwException">Specifies whether an exception should be thrown after the method fails.</param>
    Public Overloads Function UnregisterHotkey(ByVal hotkeyId As Integer, ByVal throwException As Boolean) As Hotkey
        Dim hk As Hotkey = Nothing
        Dim ex As HotkeyException
        If Me._hotkeys.ContainsKey(hotkeyId) Then
            If Not NativeMethods.UnregisterHotKey(Me._hotkeyProc.Handle, hotkeyId) Then
                Dim eCode As Integer = Marshal.GetLastWin32Error
                ex = New HotkeyException(New Win32Exception(eCode).Message)
            Else
                hk = New Hotkey(hotkeyId, Me._hotkeys.Item(hotkeyId))
                Me._hotkeys.Remove(hotkeyId)
                Return hk
            End If
        Else
            ex = New HotkeyException("The hot key id is not registered.")
        End If
        If throwException Then
            Throw ex
        End If
        Return hk
    End Function

    ''' <summary>
    ''' Replaces the hotkey data for the same hotkey id.
    ''' </summary>
    ''' <param name="hk">The hotkey whose data should be replace.</param>
    Public Function Replace(ByVal hk As Hotkey, ByVal throwException As Boolean) As Hotkey
        Dim hk1 As Hotkey = Nothing
        Dim ex As HotkeyException
        If hk.Key <> Keys.None Then
            If Me._hotkeys.ContainsKey(hk.Id) Then
                If Not NativeMethods.RegisterHotKey(Me._hotkeyProc.Handle, hk.Id, HotkeyManager.ConvertTo(hk.Modifiers), hk.Key) Then
                    Dim eCode As Integer = Marshal.GetLastWin32Error
                    ex = New HotkeyException(New Win32Exception(eCode).Message)
                Else
                    hk1 = New Hotkey(hk.Id, Me._hotkeys.Item(hk.Id))
                    Me._hotkeys.Item(hk.Id) = hk.Data
                    Return hk1
                End If
            Else
                ex = New HotkeyException("The hot key id is not registered.")
            End If
        Else
            ex = New HotkeyException("The hotkey must contain a valid key.")
        End If
        If throwException Then
            Throw ex
        End If
        Return hk1
    End Function

    ''' <summary>
    ''' Determines whether a hotkey with the specified hotkey data is available.
    ''' </summary>
    ''' <param name="hotkeyData">A System.Windows.Form.Keys that represents the hotkey data to be checked.
    ''' This parameter can contain one or combination of Keys.Ctrl, 
    ''' Keys.Alt or Keys.Shift fodifier keys and any key, combined with OR.</param>
    Public Function IsAvailable(ByVal hotkeyData As Keys) As Boolean
        Dim i As Integer = 1
        While Me._hotkeys.ContainsKey(i)
            i += 1
        End While
        Dim hk As New Hotkey(i, hotkeyData)
        Dim helper As Boolean = Me.RegisterHotKey(hk, False)
        Me.UnregisterHotKey(hk.Id, False)
        Return helper
    End Function

    ''' <summary>
    ''' Determines whether a hotkey with the specified id is registered by the HotkeyManager.
    ''' </summary>
    ''' <param name="hotkeyId">A hotkey id.</param>
    Public Function ContainsId(ByVal hotkeyId As Integer) As Boolean
        Return Me._hotkeys.ContainsKey(hotkeyId)
    End Function

    ''' <summary>
    ''' Determines whether a hotkey with the specified hotkey data is registered by the HotkeyManager.
    ''' </summary>
    ''' <param name="hotkeyData">A hotkey data.</param>
    Public Function ContainsData(ByVal hotkeyData As Keys) As Boolean
        Return Me._hotkeys.ContainsValue(hotkeyData)
    End Function

    ''' <summary>
    ''' Converts a HotkeyManager.Modifier to System.Windows.Forms.Keys (modifires).
    ''' </summary>
    ''' <param name="modifiers">A HotkeyManager.Modifier that should be converted to a System.Windows.Forms.Keys.</param>
    Private Shared Function ConvertTo(ByVal modifiers As Modifier) As Keys
        Dim myKeys As Keys = Keys.None
        If (modifiers And Modifier.Alt) = Modifier.Alt Then
            myKeys = myKeys Or Keys.Alt
        End If
        If (modifiers And Modifier.Control) = Modifier.Control Then
            myKeys = myKeys Or Keys.Control
        End If
        If (modifiers And Modifier.Shift) = Modifier.Shift Then
            myKeys = myKeys Or Keys.Shift
        End If
        Return myKeys
    End Function

    ''' <summary>
    ''' Converts a System.Windows.Forms.Keys (modifires) to HotkeyManager.Modifier.
    ''' </summary>
    ''' <param name="modifiers">A System.Windows.Forms.Keys that should be converted to a HotkeyManager.Modifier.</param>
    Private Shared Function ConvertTo(ByVal modifiers As Keys) As Modifier
        Dim myKeys As Modifier = Modifier.None
        If (modifiers And Keys.Alt) = Keys.Alt Then
            myKeys = myKeys Or Modifier.Alt
        End If
        If (modifiers And Keys.Control) = Keys.Control Then
            myKeys = myKeys Or Modifier.Control
        End If
        If (modifiers And Keys.Shift) = Keys.Shift Then
            myKeys = myKeys Or Modifier.Shift
        End If
        Return myKeys
    End Function

#Region " IDisposable Support "

    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free managed resources when explicitly called
                For Each key As Integer In Me._hotkeys.Keys
                    NativeMethods.UnregisterHotKey(Me._hotkeyProc.Handle, key)
                Next
                Me._hotkeys.Clear()
                Me._hotkeyProc.ReleaseHandle()
            End If
        End If
        Me.disposedValue = True
    End Sub

    ''' <summary>
    ''' Unregisters all hotkeys and releases all resources used by the HotkeyManager.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

#End Region

#Region " On Event "

    Protected Overridable Sub OnHotkeyPressed(ByVal e As HotkeyEventArgs)
        RaiseEvent HotkeyPressed(Me, e)
    End Sub

#End Region

#Region " NativeMethods "

    ''' <summary>
    ''' Represents win32 Api shared methods, structures, and constants.
    ''' </summary>
    <CLSCompliant(True), DebuggerNonUserCode()> _
    Private NotInheritable Class NativeMethods

#Region " Constants "

        Public Const WM_HOTKEY As Int32 = &H312
        Public Const WM_NCDESTROY As Integer = &H82

#End Region

#Region " Methods "

        <DebuggerHidden()> _
        Private Sub New()
        End Sub

        <DllImport("user32", SetLastError:=True)> _
        Public Shared Function RegisterHotKey( _
        ByVal hwnd As IntPtr, _
        ByVal id As Int32, _
        ByVal fsModifiers As Int32, _
        ByVal vk As Keys) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("user32", SetLastError:=True)> _
        Public Shared Function UnregisterHotKey( _
        ByVal hwnd As IntPtr, _
        ByVal id As Int32) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

#End Region

    End Class 'NativeMethods

#End Region

#Region " HotkeyProc Class "

    <CLSCompliant(True), DebuggerNonUserCode()> _
    Private Class HotkeyPorc
        Inherits NativeWindow

        Private _owner As HotkeyManager

        Sub New(ByVal owner As HotkeyManager)
            Me._owner = owner
        End Sub

        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            If m.Msg = NativeMethods.WM_NCDESTROY Then
                Me._owner.Dispose()
            ElseIf m.Msg = NativeMethods.WM_HOTKEY Then
                Dim wParam As UInteger = CUInt(m.LParam) >> 16
                Dim lParam As UInteger = (CUInt(m.LParam) << 16) >> 16
                Dim modifiers As Keys = HotkeyManager.ConvertTo(CType(lParam, Modifier))
                Dim hk As New Hotkey(CInt(m.WParam), CType(wParam Or modifiers, Keys))
                Me._owner.OnHotkeyPressed(New HotkeyEventArgs(hk, m.HWnd))
            End If
            MyBase.WndProc(m)
        End Sub

    End Class

#End Region

End Class

#Region " HotkeyEventArgs Class "

''' <summary>
''' Provides data for HotkeyManager events.
''' </summary>
<CLSCompliant(True), DebuggerNonUserCode()> _
Public Class HotkeyEventArgs
    Inherits EventArgs

    Private ReadOnly _hk As Hotkey
    Private ReadOnly _hwnd As IntPtr

    ''' <param name="hk">The HotkeyManager.Hotkey that contains the hot key information.</param>
    ''' <param name="handle">The window handle of the message.</param>
    Sub New(ByVal hk As Hotkey, ByVal handle As IntPtr)
        Me._hwnd = handle
        Me._hk = hk
    End Sub

    ''' <summary>
    ''' Gets the window handle of the message.
    ''' </summary>
    Public ReadOnly Property Window() As IntPtr
        Get
            Return Me._hwnd
        End Get
    End Property

    ''' <summary>
    ''' Gets HotkeyManager.Hotkey that contains the pressed hot key information.
    ''' </summary>
    Public ReadOnly Property Hotkey() As Hotkey
        Get
            Return Me._hk
        End Get
    End Property

End Class

#End Region

#Region " HotkeyException Class "

''' <summary>
''' Represents errors that occur in the HotkeyManager.
''' </summary>
<Serializable(), CLSCompliant(True), DebuggerNonUserCode()> _
Public Class HotkeyException
    Inherits Exception

    Sub New()
    End Sub

    Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Sub New(ByVal message As String, ByVal ex As Exception)
        MyBase.New(message, ex)
    End Sub

    Protected Sub New(ByVal info As Runtime.Serialization.SerializationInfo, ByVal context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub

End Class

#End Region

#Region " HotkeyCollection "

''' <summary>
''' Represents read only collection of HotkeyManager.Hotkey.
''' </summary>
<CLSCompliant(True), DebuggerNonUserCode()> _
Public Class HotkeyCollection
    Inherits ReadOnlyCollection(Of Hotkey)

    Sub New(ByVal list As Collection(Of Hotkey))
        MyBase.New(list)
    End Sub

End Class

#End Region