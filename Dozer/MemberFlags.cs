using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DouglasDwyer.Dozer;


[Flags]
public enum FieldFlags
{
    Public,
    NonPublic,
    InitOnly,
}

[Flags]
public enum PropertyFlags
{
    Auto,
    Public,
    NonPublic,
    InitOnly,
}
/*

Default setup: public fields and properties w/ get/set

Public | Fields | Properties

Alternate: public readonly fields and properties w/ get
 
Public | Readonly | Fields | Properties

I feel like one set of flags isn't enogh

A field can be:
 - Public
 - Private
 - Readonly
 - CompilerGenerated
 (the backing field of a property)

A property can be:
 - Public
 - Private
 - Readonly
 (backed by a field so auto)
 (manually implemented)

- Select between fields and properties
  - Select either auto-properties or all properties
- Of those, select between public and private
- Of those, select between non-readonly and all

 
Members {
    PublicFields,
    NonPublicFields,
    PublicProperties,
    NonPublicProperties  (then all of these InitOnly)
}

ConstructBy { Constructor, Uninitialized }

[DozerConfig(Fields = CompilerGenerated | Public | NonPublic | Readonly, Properties = Public)]

 */