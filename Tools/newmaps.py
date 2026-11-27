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
#   ~  water: nobody walks in, but you see and shoot across it
#   Q  school block    P  portable classroom    b  bleachers (shoot over)    Y  school bus
#   Z  road work (a plywood screen, ROAD CLOSED barricades)    D  dumpster (shoot over)
#   l  logs: a fallen tree or a woodpile (shoot over, one square wide)
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
XHHHHHHH..............SS...ll.jjjjjjjjjjjjHHHHHHHX  03
XHHHHHHH..hhhhhhhhhh.....2....jjjjjjjjjjjjHHHHHHHX  04
XHHHHHHH..hhhhhhhhhh......................HHHHHHHX  05
XHHHHHHH..hhhhhhhhhh......................HHHHHHHX  06
XHHHHHHH..hhhhhhhhhhXX,,XXXXXXXXXXXXXXXX..HHHHHHHX  07
XKK............X__JJJJ,,,,X_KKKKXXX............JJX  08
XKK............XXXJJJJ,,,,X_KKKK,,,..........SSJJX  09
XKK..ggg.......,,,JJJJGG,,GGKKKK,,,..........SSJJX  10
XKK..ggg.l.....,,,JJJJGG,,GGKKKK,,X.......v....JJX  11
XKK..ggg..A....X,,JJJJGG,,GGKKKK,,X............JJX  12
XKK..ggg...V.l.X,,JJJJGG,,GGKKKK,,X.XX..B......JJX  13
XKK..,,,...V...X,,JJJJGG,,GGKKKK,,X.XX.........JJX  14
XKK........V...X,,X____X,,X____X,,X........ll..JJX  15
XKK............X,,X____X,,X____X,,X...l........JJX  16
XKK............X,,XXXXXX,,XXXXXX,,X............JJX  17
X_XXXX...XXXXXXX,,X::::::::::::X,,XXXXXXXXX...XX_X  18
Xhh........X___X,,X:::::::::VV:X,,X___X........iiX  19
Xhh........XXXXX,,X::D:::::::::X,,X___X.SS.....iiX  20
Xhh....SS..X,,,,,,,::::TTTT::::X,,XXXXX........iiX  21
Xhh....SS..X,,,,,,,::::TTTT::::,,,,,,,,....SS..iiX  22
Xhh........,,,HHHHH::::TTTT::::,,,,,,,,....SS..iiX  23
Xhh.SS.....,,,HHHHH::::TTTT::::IIIIIXXX........iiX  24
Xhh........XXXHHHHH:::::::::D::IIIII__X........iiX  25
X_XXX;;XXXXXXXHHHHH:V::::::::::IIIIIXXXXXXX;;XXX_X  26
X___X;;;;;;;;;HHHHH:V::::::::::IIIII;;;;;;;;;X___X  27
X___X;;;;;;;;;HHHHH::::::::::::IIIII;;;;;;;;;X___X  28
XXXXXXX;;XXX;;X___XXXX,:::,XXXXIIIII;;XXX;;XXXXXXX  29
XX........XX;;XXXXXXXX,:::,XXXXXXXXX;;XX........XX  30
XX........|..........X,:::,XX..........|........XX  31
XX........|.......SS.,,::D,XX...................XX  32
XX.SS.....|...SS.....,,:::,XX................SS.XX  33
XX.SS.....|...SS.....X,:::,XX.....SS...|.....SS.XX  34
XX...................X,:::,,,.....SS...|........XX  35
XX...................X,:::,,,..SS......|........XX  36
XXX;;X----X..........X,:::,XX..........X----X;;XXX  37
Xhh;;X____XXXX;;IIIIII,V::,HHHHHHX;;XXXX____X;;iiX  38
Xhh;;XKKKKKKKX;;IIIIII,V::,HHHHHHX;;XJJJJJJJX;;iiX  39
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  40
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  41
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  42
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  43
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  44
Xhh;;XKKKKKKKX;;IIIIII,:::,HHHHHHX;;XJJJJJJJX;;iiX  45
XZZ,,,,,,,,Z,,,,,,,,,,,,,,,,,,V,,,,,,,,,,,,,,,,ZZX  46
XZV::::::::Z:::::::::::VVVV:::V::VV::::::::::::VZX  47
XZV::::::::Z::::::::V:::::::::V:::::::Z::::::::VZX  48
XZV::::::::Z::::::::V::::1::::V:::::::Z::::::::VZX  49
XZV::::::::::::VV:::V:::::::::::::::::Z::::::::VZX  50
XZZ,,,,,,,,,,,,,,,,,V,,,,,,,,,,,,,,,,,Z,,,,,,,,ZZX  51
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
IIIZZZZZZHHHHHHHHHiiiiiiiiiJJJJJJJJJkkkkkkkkkHHHHHHHHHHX  01
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
jjj,:ZZZZXXXXXXXXXXXX..XXXXXXXXXXXXXXXXXXXX..XXXXXX..JJX  12
jjj,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  13
jjj,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  14
jjj,::::,HHHHHHHHHH..SS....KKKKKKKKKK...SS...KKKKKX..JJX  15
jjj,::::,HHHHHHHHHH..SS....KKKKKKKKKK...SS...KKKKKX..JJX  16
KKK,::::,HHHHHHHHHH........KKKKKKKKKK........KKKKKX..JJX  17
KKK,::::,X_XgggX__X.....SS.X__GGGG__X.l......KKKKKX..JJX  18
KKKVVVV:,X_XgggX__X........X__GGGG__X........KKKKK.....X  19
KKK,::::,X_XgggX__X........X__GGGG__X........X___X.....X  20
KKK,::::,XXXgggXXXXXXX..XXXXXXGGGG__XX;;XXXXXhhhhX...llX  21
KKK,::::,;;;;;;;;;;;;;;;;ggggXGGG_GGGX;;XGGGGhhhhX.....X  22
KKK,::::,;;;;;;VV;;;;;;;;ggggXGGGXGGGX;;XGGGGhhhhX.....X  23
KKK,:1::,;;;;;;;;;;;;;;;;ggggX;GG;;;;;;;;GG;;hhhhX..2..X  24
hhh,::::,XXGGGGXXgggXX;;;;;;;;;GG;;;;;;;;GG;;;;;XX.....X  25
hhh,::::,X_GGGG_XgggXX;;;;;;;;;;;;;;;;;;;;;;;;;;;;.SS..X  26
hhh,::::,X_GGGG_XgggXX;;;;;;;;;;;;;VVVV;;;;;;;;;;;.SS..X  27
hhh,::::,X_GGGG_XgggXXTTTTTTT..TTTTTTTTTTTT..TTTXX.....X  28
hhh,::::,X______XXXXX______X........X........X___X.....X  29
hhh,:ZZZZhhhhhhhhhKKKKKKKKKK........X........IIIII.....X  30
hhh,::::,hhhhhhhhhKKKKKKKKKK...SS...X.l......IIIIIIX..hX  31
hhh,::::,hhhhhhhhhKKKKKKKKKK...SS...X.l...SS.IIIIIIX..hX  32
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
iii,::::,X.SS.....T..........SS.....X.l..........X____hX  44
iii,::::,X........T........T........X.l.......ll.X_____X  45
iii,::::,iiiiiiiiiJJJJJJJJJhhhhhhhhhX............X_____X  46
iii,::::,iiiiiiiiiJJJJJJJJJhhhhhhhhhIIIIIIIIIjjjjjjjjjjX  47
iiiZZZZZZiiiiiiiiiJJJJJJJJJhhhhhhhhhIIIIIIIIIjjjjjjjjjjX  48
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
IIIZZZZZXiiiiiSS,,,,,,,,,,........kkkkkkk........X_TTX  06
III,:::,XiiiiiSS,,,,,,,,,,XXX...............SS...X_TTX  07
III,:::,,,,,,,,,,,,A,,VV,,X_X...............SS...X_TTX  08
jjj,:::,,,,,,,,,,,,,,,,,,,HHH..XXXXXXXXXX........X_TTX  09
jjj,:::,,,,,,,,,,,,,,,,,,,HHH..IIIIIIIIIX.....2..X_TTX  10
jjj,:::,XXXXXX,oo,,,,,,,,,HHH..IIIIIIIIIX.ll.....X_TTX  11
jjj,V::,XXXXXXXXXXXXXX..XXHHH..IIIIIIIIIX........X_TTX  12
jjj,V::,X........X........HHH..IIIIIIIIIXXXXX..kkkkkkX  13
jjj,V::,X.l...............HHH..IIIIIIIIIIIIII..kkkkkkX  14
jjj,V::,X.................HHH..IIIIIIIIIIIIII..kkkkkkX  15
jjj,::V,X...SS...X..SS....HHH..IIIIIIIIIIIIII..kkkkkkX  16
KKK,::V,X...SS...X..SS....HHH..IIIIIIIIIIIIII..kkkkkkX  17
KKK,:::,.........X......l.XXX..XXXXXIIIIIIIII..kkkkkkX  18
KKK,:::,.........X......l..........XIIIIIIIII..kkkkkkX  19
KKK,:::,X........X..............ll.XIIIIIIIII....JJJJX  20
KKK,:ZZZXKKKKKKKKKXX..jjjjX........XIIIIIIIII....JJJJX  21
KKK,:ZZZXKKKKKKKKK_X..jjjjX..TT....XIIIIIIIII....JJJJX  22
KKK,:::,XKKKKKKKKK_X..jjjjX..TT..................JJJJX  23
KKKZZZ:,XKKKKKKKKK_X..jjjjX..TT..................JJJJX  24
hhhZZZ:,XKKKKKKKKK_X..jjjjX........XHHHHHHHHHXX..JJJJX  25
hhh,:::,XKKKKKKKKK_X..jjjjX.l......XHHHHHHHHH_X..JJJJX  26
hhh,::V,XKKKKKKKKKXX..jjjjXXXXXXX..XHHHHHHHHHXX..JJJJX  27
hhh,::V,X........X........jjjjjjj........X.........TTX  28
hhh,:::,X......l........l.jjjjjjj.ll.....X.........TTX  29
hhh,:::,..................jjjjjjj..................TTX  30
hhh,:::,.........X...SS...jjjjjjj...............oo.TTX  31
hhh,:::,X...SS...X...SS.............SS...X.........TTX  32
JJJZZZ:,X...SS...X..................SS...X.oo,,ggggTTX  33
JJJZZZ:,X........X........XXXXXXX........X..,B,ggggTTX  34
JJJ,:::,X........X........X_____X........XSS,,,ggggTTX  35
JJJ,:ZZZXXX..XXXXXXXXXX..XKKKKKKKXXXXX..XXSS.......TTX  36
JJJ,:ZZZX.l......X........KKKKKKK........X.........TTX  37
JJJ,:::,X.l......X......l.KKKKKKK........X.......SSTTX  38
JJJ,:::,.........X........KKKKKKK........X.......SSTTX  39
JJJ,:::,....SS...X..SS....KKKKKKK...SS...XX,,,iiiiiiTX  40
iii,:::,X...SS......SS....KKKKKKK...SS...XX,,,iiiiiijX  41
iii,:::,X.................KKKKKKK........XX,,,iiiiiijX  42
iii,V::,X......llX........KKKKKKK......l.XX,,,iiiiiijX  43
iii,V::,X........X........KKKKKKK........XX,,,iiiiiijX  44
iii,:::,XXXXX..XXXXXXX..XXXXXXXXXX..XXXXXXX,,,XjjjjjjX  45
iii,:::,,,,,,,,,ZZ,,,,,,,,,,,,,ZZ,,,,,,,,,,,,,,jjjjjjX  46
iii,::::::::::::ZZ::::::VV:::::ZZ:::::::VV:::::jjjjjjX  47
iii,:1::::::::::ZZ:ZZ:::::::ZZ:ZZ::::::::::::::jjjjjjX  48
X_X,:::::VV::::::::ZZ:::::::ZZ::::::VVVV:::::::jjjjjjX  49
X_X,,,,,,,,,,,,,,,,ZZ,,,,,,,ZZ,,,,,,,,,,,,,,,,,jjjjjjX  50
X_XXXiiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  51
X____iiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  52
XXXXXiiiiiiiiJJJJJJJJhhhhhhhhKKKKKKKKjjjjjjjjIIIIIIIIX  53
""",
      extra=[]),
 dict(name="Park Run", start="the parking lot",
      A="the picnic pavilion", B="home plate on the ball field",
      note="From the parking lot, paths run round both sides of the park through clearings in the trees. "
           "The pond is the middle: you can see and shoot across the water, but you have to go round it.",
      #           11111111112222222222333333333344444444445555
      # 012345678901234567890123456789012345678901234567890123
      grid="""
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  00
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  01
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  02
TTTT;;;;;.........ll.TTTT......TTT.......TTTTTTTTTTTTT  03
TTTT;;;;;............TTT........T.........TT......TTTT  04
TTTT;;TTT....pppp....;;;....TT..;.........T........TTT  05
TTTT;;TTT....pppp....;;;....TT..;......TT.T..T.....TTT  06
TTTT;;TTTTT..pppp....;;;....TT..T......TT.T..T.....TTT  07
TTTT;;TTTTT..A.......TTT........T..T......T.....TT.TTT  08
TTTT;;TTT..........SSTTT..T.....T..T......;.....TT.TTT  09
TTTT;;;;T..........SSTTT..T.....T.........;........TTT  10
TTTT;;;;T.ll.........TTTT......TTT.......TT........TTT  11
TTTTTT;;TTTTTTTT;;TTTTTTTTTTTTTTTTTTTTT;;TTT......TTTT  12
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTTTTT;;TTTT  13
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTTTTT;;TTTT  14
TTTTTT;;TTTT..ll...TTTTTTTTTTTTTTTTTTTT;;TTTTTTT;;TTTT  15
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTTTTT;;TTTT  16
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTT::::::::TT  17
TTTTTT;;TTTTTTTT;;TT...TT...........TTT;;TTT::::::::TT  18
TTTTTT;;TTTTT.....T........TT.......TT....TT::::V:::TT  19
TTT.......TT...............TT.......T......T:VV:V:::TT  20
TTT.....llTT.......T............ll..T.T....T::::V:::TT  21
TTT.......TT..TTTTTT...~~~~~~~~~~...T.T....T::::V:::TT  22
TTT.......TT..TTTTTT...~~~~~~~~~~...T......T::::::VVTT  23
TTT.......TT.......T...~~~TTTT~~~...,......T::::::::TT  24
TTT...SS..;;.l.....T...~~~TTTT~~~...,..SS..,::::::::TT  25
TTT...SS2.;;.......T...~~~TTTT~~~...,..SS..,::::::1:TT  26
TTT.......TT.......T...~~~TTTT~~~...,......T::::::::TT  27
TTT.......TTTTTTT..T...~~~~~~~~~~...T......TVVVV::::TT  28
TTT.......TTTTTTT..T...~~~~~~~~~~...T....T.T:::::V::TT  29
TTT.ll....TT.......T............ll..T....T.T:::::V::TT  30
TTT.......TT...l...........TT.......T......T:::::V::TT  31
TTTTTT;;TTTTT.....T........TT.......TT....TT:VV:::::TT  32
TTTTTT;;TTTTT;;TTTTT...TT...........TTT;;TTT::::::::TT  33
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTT::::::::TT  34
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTT;;TTTTTTT  35
TTTTTT;;TTTT..ll...TTTTTTTTTTTTTTTTTTTT;;TTTT;;TTTTTTT  36
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTT;;TTTTTTT  37
TTTTTT;;TTTT.......TTTTTTTTTTTTTTTTTTTT;;TTTT;;TTTTTTT  38
TTTTTT;;TTTTTTTT;;TTTTTTTTTTTTTTTTTTTTT;;TTT......TTTT  39
TTTT;;;;T............TTTT......TTT.......TT........TTT  40
TTTT;;;;T....lll.....TTT..T.....T.........;........TTT  41
TTTT;;TTT..;;;;;;;;l.TTT..T.....T..T......;..TT....TTT  42
TTTT;;TTT..|;;;;;;;l.TTT........T..T......T..TT....TTT  43
TTTT;;TTT..|;B;;;;;..;;;....TT..T......TT.T.....T..TTT  44
TTTT;;TTT..|;;;;;;;..;;;....TT..;......TT.T.....T..TTT  45
TTTT;;TTT..+---;;;;..;;;....TT..;.........T........TTT  46
TTTT;;;;;..;;;;;;;;SSTTT........T.........TT......TTTT  47
TTTT;;;;;..........SSTTTT......TTT.......TTTTTTTTTTTTT  48
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  49
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  50
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT  51
""",
      extra=[]),
 dict(name="Schoolyard", start="the bus loop",
      A="the back doors by the loading dock", B="the bleachers by the field",
      note="From the bus loop you go through the breezeway into the school's courtyard, or round either "
           "end of the building: past the portable classrooms on the west, along the field on the east.",
      #           11111111112222222222333333333344444444445555
      # 012345678901234567890123456789012345678901234567890123
      grid="""
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  00
X_XXXXXXXXXXXXXX,,,,,,,,,,,,,,,,,,,,,,XXXXXXXXXXXXXX_X  01
X_Z::::::::::::::VVV::::::1:::::::VVV::::::::::::::Z_X  02
X_Z::::::::::::::YYYYY::::::::::YYYYY::::::::::::::Z_X  03
X_X......XXXXXXX::::::::::::::::::::::XXXXXXX......X_X  04
X_X......X_____X::::::::YYYYY:::::::::X_____X......X_X  05
X_X......X_____X,,,,,,,,,,,,,,,,,,,,,,X_____X......X_X  06
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  07
X_XPPPP..QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ..SS..X_X  08
X_XPPPP..QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ..SS..X_X  09
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  10
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  11
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  12
X_X..PPPPQQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQTT....X_X  13
X_X..PPPPQQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQTT....X_X  14
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  15
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  16
X_X......QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ......X_X  17
X_XPPPP..QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ....TTX_X  18
X_XPPPP..QQQQQQQQQQQQQQQQ,,,,QQQQQQQQQQQQQQQQ....TTX_X  19
X_X......TT::::::::X,,,,,,,,,,,,,,X........TT......X_X  20
X_X......TT:D::::::X,,,,,,,,,,,bb,X........TT......X_X  21
X_X..PPPPTT::::::::,,,,,,TTTT,,,,,,........TTSS....X_X  22
X_X..PPPPTT::::::::,,,,,,TTTT,,,,,,........TTSS....X_X  23
X_X......TT:::PP:::X,,,,,TTTT,,,,,X...TT...TT......X_X  24
X_X......,,:::PP:::X,,,,,TTTT,,,,,X...TT...,,......X_X  25
X_X......,,::::::::X,,,,,,,,,,,,,,X........,,......X_X  26
X_X......TT::::::::X,SS,,,,,,,,,,,X.....SS.TT......X_X  27
X_X......TT::::::::X,,,,,,,,,,,,,,X........TT......X_X  28
X_XTT,,TTTTTT,,TTTTTTT,,TTTTTTTTTTTTTTT,,TTTTTT,,TTX_X  29
X_X......TT........X::::::::::::::X........TT......X_X  30
X_X......TT........XD:::::::::::::X........TT......X_X  31
X_X......TT........XD:::::::::::::,........TT......X_X  32
X_X..SS..TT..PPP...X::PPPP::::::::,...PPP..TT..TT..X_X  33
X_X..SS..TT..PPP...X::PPPP:PPPP:::X...PPP..TT..TT..X_X  34
X_X......TT........,:::::::PPPP:::X........TT......X_X  35
X_X......TT........,::::::::::::::X........TT......X_X  36
X_X......TT........X::::::::::::DDX........TT......X_X  37
X_X......TT........X::::::::::::::X........TT......X_X  38
X_XX...XXXXXXXXX,,XXXXXXXXXXX,,XXXXX,,XXXXXXXXX...XX_X  39
X_X,,,,,QQQQ,,,X,,XXXXX........XXXXX,,X.SS.........X_X  40
X_X,,,,,QQQQ,,,X,,,,,,,........,,,,,,,X.SS.........X_X  41
X_X,,,,,,,,,,,,X,,,,,,,...SS...,,,,,,,X....bbbb....X_X  42
X_X,,,,,,VVVV,,X,,XXXXX...SS...XXXXX,,X............X_X  43
X_X,,A,,,,,,,,,,,,X___X........X___X,,,........B...X_X  44
X_X,,,,,,,,,,,,,,,X___X...2....X___X,,,............X_X  45
X_XSS,,,,,,,oo,XXXX___X.bb.....X___XXXX.........bb.X_X  46
X__SS,,,,,,,,,,X______X........X______X............X_X  47
X___XXXXX,,XXXXXXXXXXXX,,XXXX,,XXXXXXXXXXXX,,XXXXXXX_X  48
X_______X,,,,,,,,,,,,,,,,X__X,,,,,,,,,,,,,,,,X_______X  49
X_______X,,,,,,,,,,,,,,,,X__X,,,,,,,,,,,,,,,,X_______X  50
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  51
""",
      extra=[]),
 dict(name="Creekside", start="the end of Cedar Ln",
      A="the footbridge fort", B="the back yard at 130 Elm St",
      note="A creek runs down the middle and you cross it three ways: the north footbridge, the culvert under the old road, "
           "or the south footbridge. You can see and shoot across the water, but only cross where there is a way.",
      #           11111111112222222222333333333344444444445555
      # 012345678901234567890123456789012345678901234567890123
      grid="""
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  00
IIXXXXXXXXXXX__________T~~~T________XXXXXXXXXXXXXX___X  01
IIX.........TTTTTTTTTTTT~~~TXXXXXXXXX,,,,,,,,,,,,X___X  02
IIX.........TT.........T~~~T.........,,,,,,,,,,,,X_JJX  03
IIX.........TT......TT.T~~~T.........,,,,,,A,,o,,X_JJX  04
IIX...SS....TT......TT.T~~~T.........,,,,,,,,,o,,X_JJX  05
IIX...SS....TT..........~~~T........X,,,SS,,,,,,,X_JJX  06
IIX.........TT..........,,,...SS....X,,,SS,,,,,,,X_JJX  07
IIX.............SS......,,,...SS....X,,,,,,,oo,,,X_JJX  08
jjX......SS.....SS.....T~~~T........X,,,,,,,,,,,,X_JJX  09
jjX.........TT.........T~~~T....ll..X,,,,,,,,,,,,X_JJX  10
jjX.........TT.........T~~~TTT......XXXXXXX;;XX;;XXJJX  11
jjX;;hhJJJJJJJTTTTTTTTTT~~~~~~TXXXXXXhhhh;;;;XX;;;;JJX  12
jjX;;hhJJJJJJJ_________T~~~~~~TXXXXXXhhhh;;;;XX;;;;JJX  13
jjX;;hhJJJJJJJ__TTTTTTTT~~~~~~T........hh;;IIIIXX;;JJX  14
jjX;;;;JJJJJJJ__T.......TTT~~~T........hh;;IIIIXX;;JJX  15
jjX;;;;JJJJJJJ__T.........T~~~T........hh;;IIII;;;;JJX  16
X_Xhh;;JJJJJJJ__T.....SS..,,,,,........hh;;IIII;;;;JJX  17
X_Xhh;;JJJJJJJT_T.....SS..,,,,,...SS...;;;;IIII;;XXJJX  18
X_X,,,,,,,,,,,T_T.........,,,,,...SS...;;;;IIII;;XXJJX  19
X_X:::::::::::T_T.........T~~~T........XXXXX........kX  20
X_X:::::VV::::T_T...TT....T~~~T.S......XKKK.........kX  21
X_X:::::::::::T_T...TT....T~~~T.S......XKKK.........kX  22
XXX:::::::::::TTT.........T~~~T........XKKK.....2...kX  23
Z::::::Z::::::;;;;;TTTTXXXT~~~T;;XXXXXXXKKK.........kX  24
Z::::::Z::::::;;;;;T__TTTTT~~~T;;X_JJJJJKKK.........kX  25
Z:::1::::::VV:TTTTTTTTT~~~~~~~T;;X_JJJJJKKK......l..kX  26
Z:::::::::::::T.......T~~~~~~~T;;X_JJJJJKKK..SS..l..kX  27
XXX:::::::::::;.......T~~~~~~~T;;XXJJJJJKKK..SS.....kX  28
X_X:::::::::::;.......T~~~TTTTT;;;;JJJJJKKK.........kX  29
X_X::::::VVV::T..SS...T~~~T___X;;;;JJJJJKKK.........kX  30
X_X:::::::::::T..SS...T~~~T___XXX;;JJJJJGGGGiiXX;;HHHX  31
X_X,,,,,,,,,,,T.......T~~~T_____X;;JJJJJGGGGii;;;;HHHX  32
X_Xii;;KKKKKKKT.......T~~~T_____X;;JJJJJGGGGii;;;;HHHX  33
KKXii;;KKKKKKKT.....SST~~~T_____X;;JJJJJGGGGii;;XXHHHX  34
KKX;;;;KKKKKKKT.......T~~~T_____X;;;;;;;GGGGii;;;;HHHX  35
KKX;;;;KKKKKKKT.......T~~~T_____X;;;;;;;GGGGii;;;;HHHX  36
KKX;;iiKKKKKKKTTTT;;TTT~~~T_____XXXXXX;;X___iiii;;HHHX  37
KKX;;iiKKKKKKKTTTT;;TTT~~~T__________X;;XXXXXXXX;;HHHX  38
KKX;;iiKKKKKKK........T~~~TTTTXXXXXXXX......ll....XHHX  39
KKX.........TT......TTT~~~~~~T.......X............XHHX  40
KKX.........TT........T~~~~~~T.......X............XHHX  41
hhX...................T~~~~~~T.SS....X.......B....XHHX  42
hhX...TT.........TT....TTT~~~T.SS....X............XHHX  43
hhX...TT....TT...TT.......,,,........X.........wwwXHHX  44
hhX.........TT............,,,........X.........www_HHX  45
hhX.........TT...........T~~~T...........SS....wwwXHHX  46
hhX......TT.TT...........T~~~TTT.........SS.......XHHX  47
hhX.........TTTTTTTTTTTXXT~~~TTT.........SS.......XHHX  48
hhXXXXXXXXXXT____________T~~~T.......XXXXXXXXXXXXXX__X  49
X________________________T~~~TXXXXXXXX_______________X  50
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX  51
""",
      extra=[]),
]
