using System;
using System.Linq;
using System.Collections.Generic;

namespace MindustryCompiler.Code
{
    public class Memory
    {
        public List<MemStruct> memStructs;
        public int size, stackSize;
        public Dictionary<string, int> mapping;



        public Memory()
        {
            memStructs = new List<MemStruct>();
            size = 0;
            mapping = new Dictionary<string, int>();
        }



        public void AddMemStruct(MemStruct memStruct)
        {
            Console.WriteLine($"Added memory struct {memStruct.name} of size {memStruct.size} at {memStruct.start}.");

            memStructs.Add(memStruct);
            size += memStruct.size;
        }
        public int AddMapping(string name) => AddMapping(name, 1);
        public int AddMapping(string name, int allocSize)
        {
            int lowestUsed = -1;

            int[] indexes = mapping.Values.ToArray();

            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] > lowestUsed)
                    lowestUsed = indexes[i];
            }

            if (lowestUsed + allocSize < size)
            {
                lowestUsed++;

                for (int i = 0; i < allocSize; i++)
                {
                    if (i == 0)
                        mapping.Add(name, lowestUsed);
                    else
                        mapping.Add($"{name}{i}", lowestUsed + i);
                }

                Console.WriteLine($"Allocated {name} of size {allocSize} at {lowestUsed}");
                return lowestUsed;
            }

            throw new OutOfMemoryException($"Out of memory for allocation: lowestUsed={lowestUsed} allocSize={allocSize} memSize={size} structCount={memStructs.Count}");
        }
        public bool IsInRange(int index) => index >= 0 && index < size;
        public Dictionary<string, (int, int)> GetStructsBaseTop()
        {
            Dictionary<string, (int, int)> baseTop = new Dictionary<string, (int, int)>();

            for (int i = 0; i < memStructs.Count; i++)
            {
                baseTop.Add(memStructs[i].name, (memStructs[i].start, memStructs[i].start + memStructs[i].size));
            }

            return baseTop;
        }
    }
}
