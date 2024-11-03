using Dogshouseservice.Models;

namespace Dogshouseservice.Helpers
{
    public class DogModelComparer : IComparer<DogModel>
    {
        private readonly DogSortingAttribute _attribute;
        private readonly string _order;

        public DogModelComparer(DogSortingAttribute attribute, string order)
        {
            _attribute = attribute;
            _order = order;
        }

        public int Compare(DogModel? x, DogModel? y)
        {
            if (x == null || y == null) return 0;

            int comparisonResult = _attribute switch
            {
                DogSortingAttribute.Weight => x.Weight.CompareTo(y.Weight),
                DogSortingAttribute.TailLength => x.TailLength.CompareTo(y.TailLength),
                _ => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
            };

            return _order == "desc" ? -comparisonResult : comparisonResult;
        }
    }
}
