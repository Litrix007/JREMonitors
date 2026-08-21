using System;

namespace JREMonitors.E233.Buttons
{
    public interface IClickable
    {
        event Action OnClick;
    }
}