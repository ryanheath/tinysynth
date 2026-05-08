namespace TinySynth.App.Input;

internal interface IInputDevice
{
    void Update(InputDeviceContext context, ICollection<InputAction> actions);
}
