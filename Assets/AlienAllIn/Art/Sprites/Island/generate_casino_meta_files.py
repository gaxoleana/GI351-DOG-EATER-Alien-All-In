SPRITES = [
    ("Casino_Tile_A.png", "c1000000000000000000000000000001", "Casino_Tile_A_0", 2001),
    ("Casino_Tile_B.png", "c1000000000000000000000000000002", "Casino_Tile_B_0", 2002),
    ("Casino_Cliff_Front.png", "c1000000000000000000000000000003", "Casino_Cliff_Front_0", 2003),
    ("Casino_Cliff_Corner_Left.png", "c1000000000000000000000000000004", "Casino_Cliff_Corner_Left_0", 2004),
    ("Casino_Cliff_Corner_Right.png", "c1000000000000000000000000000005", "Casino_Cliff_Corner_Right_0", 2005),
    ("Casino_Cliff_Side_Left.png", "c1000000000000000000000000000006", "Casino_Cliff_Side_Left_0", 2006),
    ("Casino_Cliff_Side_Right.png", "c1000000000000000000000000000007", "Casino_Cliff_Side_Right_0", 2007),
    ("Casino_Rim_Top.png", "c1000000000000000000000000000008", "Casino_Rim_Top_0", 2008),
    ("Casino_Island_Shadow.png", "c1000000000000000000000000000009", "Casino_Island_Shadow_0", 2009),
    ("Character_Drop_Shadow.png", "c100000000000000000000000000000a", "Character_Drop_Shadow_0", 2010),
    ("Floating_Casino_Chip_Island.png", "c100000000000000000000000000000b", "Floating_Casino_Chip_Island_0", 2011),
]

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {internal_id}
    second: {sprite_name}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 256
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: 256
        height: 256
      alignment: 9
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {guid}
      internalID: {internal_id}
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      {sprite_name}: {internal_id}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

DIR = r"D:\WorkForgit\GI351-DOG-EATER-Alien-All-In\Assets\AlienAllIn\Art\Sprites\Island"
for filename, guid, sprite_name, internal_id in SPRITES:
    meta_path = f"{DIR}\\{filename}.meta"
    content = META_TEMPLATE.format(guid=guid, sprite_name=sprite_name, internal_id=internal_id)
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)
    print(f"Wrote meta: {meta_path}")
