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
#   w  above-ground pool (shoot over)    p  floor under a party tent    s  sand
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
 dict(name="Pine St Backyards", start="Pine St",
      A="the birthday party behind 122 Cedar Ln", B="the pool behind 156 Birch Ln",
      note="Two rows of back yards run east from Pine St, gate to gate, with the garage alley between them. Each site "
           "has three doors: the last yard's gate, its lobby yard off the alley, and the defenders' side path.",
      #           1111111111222222222233333333334444444444555555
      # 01234567890123456789012345678901234567890123456789012345
      grid="""
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  00
IIIXXXXXXHHHHHHHHHiiiiiiiiiJJJJJJJJJkkkkkkkkkHHHHHHHHHHX  01
III,::::,HHHHHHHHHiiiiiiiiiJJJJJJJJJkkkkkkkkkHHHHHHHHHHX  02
III,::::,HHHHHHHHHiiiiiiiiiJJJJJJJJJk............HHHHHHX  03
III,::::,X........T........T........X.........SS.X___JJX  04
III,::::,X........T.SS..............X....pppp.SS.X___JJX  05
III,::::,X..www...T.................X....pppp....XXXXJJX  06
III,::::,...www...T...SS...T..SS....X....ppppA.......JJX  07
III,::::,.........T...SS...T..SS....X................JJX  08
jjj,::::,X.................T............X.oo.....XX..JJX  09
jjj,::::,X.....SS..........T...SS.......X.....oo.XX..JJX  10
jjj,::::,X........T........T............X........XX..JJX  11
jjj,:VVVVXXXXXXXXXXXX..XXXXXXXXXXXXXXXXXXXX..XXXXXX..JJX  12
jjj,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  13
jjj,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  14
jjj,::::,HHHHHHHHHH..SS....KKKKKKKKKK...SS...KKKKKX..JJX  15
jjj,::::,HHHHHHHHHH..SS....KKKKKKKKKK...SS...KKKKKX..JJX  16
KKK,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  17
KKK,::::,X_XgggX__X.....SS.X__GGGG__X.o......KKKKKX..JJX  18
KKKVVVV:,X_XgggX__X........X__GGGG__X........KKKKK.....X  19
KKK,::::,X_XgggX__X........X__GGGG__X........X___X.....X  20
KKK,::::,XXXgggXXXXXXX..XXXXXXGGGG__XX;;XXXXXhhhhX...ooX  21
KKK,::::,;;;;;;;;;;;;;;;;ggggXGGG_GGGX;;XGGGGhhhhX.....X  22
KKK,::::,;;;;;;VV;;;;;;;;ggggXGGGXGGGX;;XGGGGhhhhX.....X  23
KKK,:1::,;;;;;;;;;;;;;;;;ggggX;SS;;;;;;;;SS;;hhhhX..2..X  24
hhh,::::,XXGGGGXXgggXX;;;;;;;;;SS;;;;;;;;SS;;;;;XX.....X  25
hhh,::::,X_GGGG_XgggXX;;;;;;;;;;;;;;SS;;;;;;;;;;;;.SS..X  26
hhh,::::,X_GGGG_XgggXX;;;;;;;;;;;;;;SS;;;;;;;;;;;;.SS..X  27
hhh,::::,X_GGGG_XgggXXTTTTTTT..TTTTTTTTTTTT..TTTXX.....X  28
hhh,::::,X______XXXXX______X........X........X___X.....X  29
hhh,:VVVVhhhhhhhhhKKKKKKKKKK........X........IIIII.....X  30
hhh,::::,hhhhhhhhhKKKKKKKKKK...SS...X.o......IIIIIIX..hX  31
hhh,::::,hhhhhhhhhKKKKKKKKKK...SS...X.o...SS.IIIIIIX..hX  32
JJJ,::::,hhhhhhhhhKKKKKKKKKK........X.....SS.IIIIIIX..hX  33
JJJ,::::,hhhhhhhhhKKKKKKKKKK........X........IIIIIIX..hX  34
JJJ,::::,hhhhhhhhhKKKKKKKKKK.SS.....X........IIIIIIX..hX  35
JJJVVVV:,hhhhhhhhhKKKKKKKKKK........X........IIIIIIX..hX  36
JJJ,::::,XXXXXXXXXXXXXXXXXXXXXXX..XXXXXXX..XXIIIIIIX..hX  37
JJJ,::::,X........T........T........X............X_X..hX  38
JJJ,::::,X..........S......T..................SS.X_X..hX  39
JJJ,::::,X.................T...SS.............SS.XXX..hX  40
iii,::::,....SS...T...ppp..T...SS........wwww.........hX  41
iii,::::,....SS...T...ppp..T........X....wwwwB........hX  42
iii,::::,X........T...ppp...........X....wwww....XXXXXhX  43
iii,::::,X.SS.....T..........SS.....X.o..........X____hX  44
iii,::::,X........T........T........X.o.......oo.X_____X  45
iii,::::,iiiiiiiiiJJJJJJJJJhhhhhhhhhX............X_____X  46
iii,::::,iiiiiiiiiJJJJJJJJJhhhhhhhhhIIIIIIIIIjjjjjjjjjjX  47
iiiXXXXXXiiiiiiiiiJJJJJJJJJhhhhhhhhhIIIIIIIIIjjjjjjjjjjX  48
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  49
""",
      extra=[]),
 dict(name="Hickory Corner", start="the corner of Hickory Rd and Birch Ln",
      A="the motorhome at 100 Hickory Rd", B="the garage sale at 166 Birch Ln",
      note="Hickory Rd runs north from the corner and Birch Ln runs east, each bending into a site at its far end. The "
           "back yards inside the corner are the middle: yard to yard to the old oak, where the defenders meet you.",
      #           11111111112222222222333333333344444444445555
      # 012345678901234567890123456789012345678901234567890123
      grid="""
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  00
III______TTTTTTTTTTTTTTTTTJJJJJJJJkkkkkkkTTTTTTTTTTTTX  01
III______iiiiiTTTTTTTTTTTTJJJJJJJJkkkkkkkTTTTTTTTTTTTX  02
III______iiiii,,,,,,,,,,ooJJJJJJJJkkkkkkk__________TTX  03
III______iiiii,,,,,,,,,,,,JJJJJJJJkkkkkkkXXXXXXXXX_TTX  04
III______iiiii,,,VVVVV,,,,........kkkkkkk........X_TTX  05
IIIXXXXXXiiiiiSS,,,,,,,,,,........kkkkkkk........X_TTX  06
III,:::,XiiiiiSS,,,,,,,,,,XXX...............SS...X_TTX  07
III,:::,,,,,,,,,,,,A,,VV,,X_X...............SS...X_TTX  08
jjj,:::,,,,,,,,,,,,,,,,,,,HHH..XXXXXXXXXX........X_TTX  09
jjj,:::,,,,,,,,,,,,,,,,,,,HHH..IIIIIIIIIX.....2..X_TTX  10
jjj,:::,XXXXXX,oo,,,,,,,,,HHH..IIIIIIIIIX.oo.....X_TTX  11
jjj,V::,XXXXXXXXXXXXXX..XXHHH..IIIIIIIIIX........X_TTX  12
jjj,V::,X........X........HHH..IIIIIIIIIXXXXX..kkkkkkX  13
jjj,V::,X.o...............HHH..IIIIIIIIIIIIII..kkkkkkX  14
jjj,V::,X.................HHH..IIIIIIIIIIIIII..kkkkkkX  15
jjj,::V,X...SS...X..SS....HHH..IIIIIIIIIIIIII..kkkkkkX  16
KKK,::V,X...SS...X..SS....HHH..IIIIIIIIIIIIII..kkkkkkX  17
KKK,:::,.........X......o.XXX..XXXXXIIIIIIIII..kkkkkkX  18
KKK,:::,.........X......o..........XIIIIIIIII..kkkkkkX  19
KKK,:::,X........X..............oo.XIIIIIIIII....JJJJX  20
KKK,:TTTXKKKKKKKKKXX..jjjjX........XIIIIIIIII....JJJJX  21
KKK,:TTTXKKKKKKKKK_X..jjjjX..TT....XIIIIIIIII....JJJJX  22
KKK,:::,XKKKKKKKKK_X..jjjjX..TT..................JJJJX  23
KKKTTT:,XKKKKKKKKK_X..jjjjX..TT..................JJJJX  24
hhhTTT:,XKKKKKKKKK_X..jjjjX........XHHHHHHHHHXX..JJJJX  25
hhh,:::,XKKKKKKKKK_X..jjjjX.o......XHHHHHHHHH_X..JJJJX  26
hhh,::V,XKKKKKKKKKXX..jjjjXXXXXXX..XHHHHHHHHHXX..JJJJX  27
hhh,::V,X........X........jjjjjjj........X.........TTX  28
hhh,:::,X......o........o.jjjjjjj.oo.....X.........TTX  29
hhh,:::,..................jjjjjjj..................TTX  30
hhh,:::,.........X...SS...jjjjjjj...............oo.TTX  31
hhh,:::,X...SS...X...SS.............SS...X.........TTX  32
JJJTTT:,X...SS...X..................SS...X.oo,,ggggTTX  33
JJJTTT:,X........X........XXXXXXX........X..,B,ggggTTX  34
JJJ,:::,X........X........X_____X........XSS,,,ggggTTX  35
JJJ,:TTTXXX..XXXXXXXXXX..XKKKKKKKXXXXX..XXSS.......TTX  36
JJJ,:TTTX.o......X........KKKKKKK........X.........TTX  37
JJJ,:::,X.o......X......o.KKKKKKK........X.......SSTTX  38
JJJ,:::,.........X........KKKKKKK........X.......SSTTX  39
JJJ,:::,....SS...X..SS....KKKKKKK...SS...XX,,,iiiiiiTX  40
iii,:::,X...SS......SS....KKKKKKK...SS...XX,,,iiiiiijX  41
iii,:::,X.................KKKKKKK........XX,,,iiiiiijX  42
iii,V::,X......ooX........KKKKKKK......o.XX,,,iiiiiijX  43
iii,V::,X........X........KKKKKKK........XX,,,iiiiiijX  44
iii,:::,XXXXX..XXXXXXX..XXXXXXXXXX..XXXXXXX,,,XjjjjjjX  45
iii,:::,,,,,,,,,TT,,,,,,,,,,,,,TT,,,,,,,,,,,,,,jjjjjjX  46
iii,::::::::::::TT::::::VV:::::TT:::::::VV:::::jjjjjjX  47
iii,:1::::::::::TT:TT:::::::TT:TT::::::::::::::jjjjjjX  48
X_X,:::::VV::::::::TT:::::::TT::::::VVVV:::::::jjjjjjX  49
X_X,,,,,,,,,,,,,,,,TT,,,,,,,TT,,,,,,,,,,,,,,,,,jjjjjjX  50
X_XXXiiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  51
X____iiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  52
XXXXXiiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  53
""",
      extra=[]),
]
