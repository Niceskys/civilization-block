using System;
using System.Linq;

namespace WenMingBlocks.Runtime.Authority
{
    public static class StorageCapacityRules
    {
        public const int BaseSharedCapacity = 2000;

        public static long UsedCapacity(ResourceState resources)
        {
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            return resources.Items.Values.Where(stack => stack != null).Sum(stack => (long)stack.Amount);
        }

        public static long ReservedCapacity(ResourceState resources)
        {
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            return resources.Items.Values.Where(stack => stack != null).Sum(stack => (long)stack.IncomingReservedAmount);
        }

        public static int SharedFreeCapacity(ResourceState resources)
        {
            long free = Math.Max(0L, resources.SharedCapacity - UsedCapacity(resources) - ReservedCapacity(resources));
            return (int)Math.Min(int.MaxValue, free);
        }

        public static int ResourceFreeCapacity(ResourceState resources, ResourceStack stack)
        {
            if (stack == null) return 0;
            int categoryFree = Math.Max(0, stack.Capacity - stack.Amount - stack.IncomingReservedAmount);
            return Math.Min(categoryFree, SharedFreeCapacity(resources));
        }

        public static bool CanRemoveBonus(ResourceState resources, int bonus)
        {
            if (bonus <= 0) return true;
            long reducedCapacity = (long)resources.SharedCapacity - bonus;
            return reducedCapacity >= 0 &&
                   UsedCapacity(resources) + ReservedCapacity(resources) <= reducedCapacity;
        }

        public static void AddBonus(ResourceState resources, int bonus)
        {
            if (bonus < 0) throw new ArgumentOutOfRangeException(nameof(bonus));
            resources.SharedCapacity = checked(resources.SharedCapacity + bonus);
        }

        public static void RemoveBonus(ResourceState resources, int bonus)
        {
            if (bonus < 0) throw new ArgumentOutOfRangeException(nameof(bonus));
            resources.SharedCapacity = Math.Max(0, resources.SharedCapacity - bonus);
        }
    }
}
