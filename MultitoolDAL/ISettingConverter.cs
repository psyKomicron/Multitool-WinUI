namespace Multitool.DAL
{
    public interface ISettingConverter<in TIn, out TOut>
    {
        /// <summary>
        /// Converts <paramref name="toConvert"/> to a type that can be saved by an <see cref="ISettings"/> instance.
        /// </summary>
        /// <param name="toConvert"><see cref="object"/> to convert</param>
        /// <returns>The converted <see cref="object"/></returns>
        TOut Convert(TIn toConvert);
    }
}
