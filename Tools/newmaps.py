# newmaps.py: the remade maps, each one a drawing made of letters.
#
# One letter is one 2 m square and north is at the top. blueprint.py has the full list of
# letters and checks every drawing; export_cs.py writes the result into the game, where a
# drawing here replaces the old map of the same name. The short version:
#
#   . grass   , pavement   : street   ; dirt        1 attackers start   2 defenders start
#   A B  site middles      g  open garage floor     ^ v < >  treehouse (arrow = ramp)
#   H I J K  two-storey houses    h i j k  one-storey houses    G  shut garage    S  shed
#   X  hedge    T  trees    - | +  board fence    o  crates (shoot over)    V  vehicle
#   _  a closed yard nobody can get into
#
# The number after each row is only a label (row 00 is the north edge), so a row is easy
# to find. `extra` is for things that do not sit on the squares: the same props the old
# maps list as `cover` (see props.py), in meters from the drawing's south-west corner.
#
# After changing a drawing:   python3 blueprint.py "Willow Court"    (checks it, draws it,
# and measures it against the audit's spec), then   python3 maps5.py && python3 export_cs.py

MAPS = [
 dict(name="Willow Court", start="Birch Ln",
      A="the open garage at 104 Willow Ct", B="the treehouse behind 110 Willow Ct",
      note="Up the stem of Willow Ct into the cul-de-sac, or round either side through the back yards. "
           "Each site has three doors: one from its lobby yard, one from the cul-de-sac, one from the defenders' side.",
      #         1111111111222222222233333333334444444444
      # 01234567890123456789012345678901234567890123456789
      grid="""
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  00
XHHHHHHHXXXXXXXXXXXXXXXXXXXXXXjjjjjjjjjjjjHHHHHHHX  01
XHHHHHHH..............SS......jjjjjjjjjjjjHHHHHHHX  02
XHHHHHHH..............SS...oo.jjjjjjjjjjjjHHHHHHHX  03
XHHHHHHH..hhhhhhhhhh.....2....jjjjjjjjjjjjHHHHHHHX  04
XHHHHHHH..hhhhhhhhhh......................HHHHHHHX  05
XHHHHHHH..hhhhhhhhhh......................HHHHHHHX  06
XHHHHHHH..hhhhhhhhhhXX,,XXXXXXXXXXXXXXXX..HHHHHHHX  07
XKK............X__JJJJ,,,,X_KKKKXXX............JJX  08
XKK............XXXJJJJ,,,,X_KKKK,,,..........SSJJX  09
XKK..ggg.......,,,JJJJGG,,GGKKKK,,,..........SSJJX  10
XKK..ggg.o.....,,,JJJJGG,,GGKKKK,,X.......v....JJX  11
XKK..ggg..A....X,,JJJJGG,,GGKKKK,,X............JJX  12
XKK..ggg...V.o.X,,JJJJGG,,GGKKKK,,X.XX..B......JJX  13
XKK..,,,...V...X,,JJJJGG,,GGKKKK,,X.XX.........JJX  14
XKK........V...X,,X____X,,X____X,,X........oo..JJX  15
XKK............X,,X____X,,X____X,,X...o........JJX  16
XKK............X,,XXXXXX,,XXXXXX,,X............JJX  17
X_XXXX...XXXXXXX,,X::::::::::::X,,XXXXXXXXX...XX_X  18
Xhh........X___X,,X:::::::::VV:X,,X___X........iiX  19
Xhh........XXXXX,,X::o:::::::::X,,X___X.SS.....iiX  20
Xhh....SS..X,,,,,,,::::TTTT::::X,,XXXXX........iiX  21
Xhh....SS..X,,,,,,,::::TTTT::::,,,,,,,,....SS..iiX  22
Xhh........,,,HHHHH::::TTTT::::,,,,,,,,....SS..iiX  23
Xhh.SS.....,,,HHHHH::::TTTT::::IIIIIXXX........iiX  24
Xhh........XXXHHHHH:::::::::o::IIIII__X........iiX  25
X_XXX;;XXXXXXXHHHHH:V::::::::::IIIIIXXXXXXX;;XXX_X  26
X___X;;;;;;;;;HHHHH:V::::::::::IIIII;;;;;;;;;X___X  27
X___X;;;;;;;;;HHHHH::::::::::::IIIII;;;;;;;;;X___X  28
XXXXXXX;;XXX;;X___XXXXX::::XXXXIIIII;;XXX;;XXXXXXX  29
XX........XX;;XXXXXXXXX::::XXXXXXXXX;;XX........XX  30
XX........|..........XX::::XX..........|........XX  31
XX........|.......SS.,,:::oXX...................XX  32
XX.SS.....|...SS.....,,::::XX................SS.XX  33
XX.SS.....|...SS.....XX::::XX.....SS...|.....SS.XX  34
XX...................XX::::,,.....SS...|........XX  35
XX...................XX::::,,..SS......|........XX  36
XXX;;X----X..........XX::::XX..........X----X;;XXX  37
Xhh;;X____XXXX;;IIIIIIXV:::XHHHHHX;;XXXX____X;;iiX  38
Xhh;;XKKKKKKKX;;IIIIIIXV:::XHHHHHX;;XJJJJJJJX;;iiX  39
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  40
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  41
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  42
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  43
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  44
Xhh;;XKKKKKKKX;;IIIIIIX::::XHHHHHX;;XJJJJJJJX;;iiX  45
X_X,,,,,,,,V,,,,,,,,,,,,,,,,,,V,,,,,,,,,,,,,,,,X_X  46
X_V::::::::V:::::::::::VVVV:::V::VV::::::::::::V_X  47
X_V::::::::V::::::::V:::::::::V:::::::V::::::::V_X  48
X_V::::::::V::::::::V::::1::::V:::::::V::::::::V_X  49
X_V::::::::::::VV:::V:::::::::::::::::V::::::::V_X  50
X_X,,,,,,,,,,,,,,,,,V,,,,,,,,,,,,,,,,,V,,,,,,,,X_X  51
XhhhhhhhhIIIIIIIIjjjjjjjjHHHHHHHHiiiiiiiiJJJJJJJJX  52
XhhhhhhhhIIIIIIIIjjjjjjjjHHHHHHHHiiiiiiiiJJJJJJJJX  53
""",
      extra=[]),
]
