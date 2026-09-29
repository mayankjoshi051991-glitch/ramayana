namespace Ramayana.Combat
{
    public interface IArrowTarget
    {
        // Return true if the arrow should be consumed.
        bool OnArrowHit(int damage);
    }
}
