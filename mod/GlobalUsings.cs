// Project-wide usings.
//
// `MegaCrit.Sts2.Core.Models` holds the model base types every piece of content touches
// (`CardModel`, `ActModel`, `RelicModel`, `PowerModel`, `ModelDb`), and
// `MegaCrit.Sts2.Core.Events` holds `EventOption`, which every ported event builds.
// Both were being forgotten file-by-file, producing a steady stream of CS0246 build breaks,
// so they are declared once here instead.
global using MegaCrit.Sts2.Core.Events;
global using MegaCrit.Sts2.Core.Models;
