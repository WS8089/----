namespace BearPi.TiltMaze
{
    public enum GameState { MainMenu, Ready, Playing, Paused, Failed, Completed }

    public enum FailureReason { None, FellOffBoard, DurabilityDepleted }
}
