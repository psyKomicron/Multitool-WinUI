namespace Multitool.DAL.Completion
{
    /// <summary>
    /// Defines a method to complete pathes.
    /// </summary>
    public interface IPathCompletor
    {
        /// <summary>
        /// Completes a path by a list of possible choices.
        /// </summary>
        /// <param name="input">Path input</param>
        /// <returns>A list of possible choices</returns>
        string[] Complete(string input);
    }
}
