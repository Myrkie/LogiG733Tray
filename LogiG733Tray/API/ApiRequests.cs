namespace LogiG733Tray.API
{
    public record PowerOffRequest(int Minutes);
    
    public record LightRequest(
        string Target,
        byte? UpperR = null,
        byte? UpperG = null,
        byte? UpperB = null,
        byte? LowerR = null,
        byte? LowerG = null,
        byte? LowerB = null
    );
}