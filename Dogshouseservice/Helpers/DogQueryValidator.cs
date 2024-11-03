using Dogshouseservice.Constants;

namespace Dogshouseservice.Helpers
{
    public class DogQueryValidator
    {
        private readonly HashSet<string> _allowedAttributes = new() { "name", "weight", "tail_length" };

        public bool ValidateSortingParameters(string attribute, string order, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0 || pageSize <= 0)
                return false;

            if (!_allowedAttributes.Contains(attribute.ToLower()))
                return false;

            if (order != SortingConstants.Ascending && order != SortingConstants.Descending)
                return false;

            return true;
        }
    }
}