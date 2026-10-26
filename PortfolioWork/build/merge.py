from pathlib import Path
import zipfile, xml.etree.ElementTree as E
root=Path(__file__).parent
source=Path('C:/Users/user/Downloads/Portfolio_박성희_수정본.pptx')
ns='http://schemas.openxmlformats.org/package/2006/relationships'
E.register_namespace('',ns)
with zipfile.ZipFile(source) as original, zipfile.ZipFile(root/'replacement.pptx') as replacement:
    rels=E.fromstring(replacement.read('ppt/slides/_rels/slide1.xml.rels'))
    original_rels=E.fromstring(original.read('ppt/slides/_rels/slide17.xml.rels'))
    layout=next(r.attrib['Target'] for r in original_rels if r.attrib['Type'].endswith('/slideLayout'))
    extras={}
    for r in list(rels):
        kind=r.attrib['Type'].split('/')[-1]
        if kind=='slideLayout': r.set('Target',layout)
        elif kind=='image':
            path=r.attrib['Target'].lstrip('/')
            destination='ppt/media/portfolio_3d_gameplay.png'
            extras[destination]=(root/'gameplay-final.png').read_bytes()
            r.set('Target','../media/portfolio_3d_gameplay.png')
        else: rels.remove(r)
    changed={'ppt/slides/slide17.xml':replacement.read('ppt/slides/slide1.xml'),
             'ppt/slides/_rels/slide17.xml.rels':E.tostring(rels,encoding='utf-8',xml_declaration=True)}
    with zipfile.ZipFile(root/'candidate.pptx','w',zipfile.ZIP_DEFLATED) as out:
        for item in original.infolist(): out.writestr(item,changed.get(item.filename,original.read(item.filename)))
        for name,data in extras.items(): out.writestr(name,data)
with zipfile.ZipFile(source) as a,zipfile.ZipFile(root/'candidate.pptx') as b:
    differences=[n for n in a.namelist() if a.read(n)!=b.read(n)]
    assert set(differences)==set(changed),differences
    print('Only slide 17 and its relationships changed; all other source entries preserved.')

