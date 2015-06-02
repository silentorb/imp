module Summoning.Painter

let render_stroke stroke =
    "hello"

let render_list strokes =
    Seq.collect render_stroke strokes

