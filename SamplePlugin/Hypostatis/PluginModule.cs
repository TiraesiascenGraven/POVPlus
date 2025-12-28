using System;

namespace Hypostasis;

public abstract class PluginModule
{
    private bool? isValid;
    public bool IsValid
    {
        get
        {
            try
            {
                if (isValid == null)
                    DalamudApi.SigScanner.Inject(this);
                return isValid ?? (isValid = Validate()).Value;
            }
            catch (Exception e)
            {
                DalamudApi.LogError($"Error validating {this}", e);
                return (isValid = false).Value;
            }
        }
        set
        {
            if (!value)
                Invalidate();
        }
    }

    public bool IsEnabled { get; set; }

    public virtual bool ShouldEnable => true;

    protected virtual bool Validate() => true;
    protected virtual void Enable() { }
    protected virtual void Disable() { }
    public virtual void Dispose() { }

    public void Toggle()
    {
        if (!IsValid) return;

        if (!IsEnabled)
        {
            Enable();
            IsEnabled = true;
        }
        else
        {
            Disable();
            IsEnabled = false;
        }
    }

    private void Invalidate()
    {
        if (!IsValid) return;
        Disable();
        isValid = false;
    }
}