namespace WpfApp;

public partial class Class1 : global::System.Windows.Controls.Control
{
    private static partial int CoerceAge(int? value)
    {
        throw new NotImplementedException();
    }

    private static partial bool IsAgeValid(int? value)
    {
        throw new NotImplementedException();
    }
}

partial class Class1
{
    /// <summary>
    /// Identifies the <see cref="MyProperty"/> dependency property.<br/>
    /// Default value: default(int)
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    public static readonly global::System.Windows.DependencyProperty MyPropertyProperty =
        global::System.Windows.DependencyProperty.Register(
            "MyProperty",
            typeof(int),
            typeof(global::WpfApp.Class1),
            new global::System.Windows.PropertyMetadata(
                0,
                propertyChangedCallback: static (s, e) =>
                {
                    if (s is not global::WpfApp.Class1 control)
                        return;

                    control.OnaChanged();
                    control.OnaChanged((int)e.NewValue);
                    control.OnaChanged((int)e.OldValue, (int)e.NewValue);
                },
                coerceValueCallback: static (s, value) => CoerceAge((int?)value)
            ),
            validateValueCallback: static value => IsAgeValid((int?)value)
        );

    /// <summary>
    /// ddeee<br/>
    /// Default value: default(int)
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.ComponentModel.Bindable(true)]
    [global::System.ComponentModel.Browsable(false)]
    [global::System.ComponentModel.Category("aa")]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.ComponentModel.Description("年龄")]
    public int MyProperty
    {
        get { return (int)GetValue(MyPropertyProperty); }
        set { SetValue(MyPropertyProperty, value); }
    }

    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    private static partial int CoerceAge(int? value);

    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    private static partial bool IsAgeValid(int? value);

    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    partial void OnaChanged();

    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    partial void OnaChanged(int newValue);

    [global::System.CodeDom.Compiler.GeneratedCode("DependencyPropertyGenerator", "1.0.0.0")]
    partial void OnaChanged(int oldValue, int newValue);
}