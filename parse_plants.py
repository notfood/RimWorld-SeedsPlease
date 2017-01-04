#!/bin/env python

import sys
from lxml import etree
#import xml.etree.ElementTree as etree


if len(sys.argv) < 2:
    exit(1)

seeds = etree.Element("Defs")
recipes = etree.Element("Defs")

for file in sys.argv[1:]:
    tree = etree.parse(file)
    root = tree.getroot()
    for child in root.findall('ThingDef'):
        defName = child.find('defName')
        if defName is None:
            continue
        
        name = defName.text.replace('Plant', '')

        plant = child.find('plant')
        if plant is None:
            continue
        
        if plant.find('harvestYield') is None:
            continue
        
        print(name + " " + child.get('ParentName'))

        sthingDef = etree.SubElement(seeds, 'SeedsPlease.SeedDef')
        sthingDef.set('ParentName', 'SeedBase')
        sdefName = etree.SubElement(sthingDef, 'defName')
        sdefName.text = 'Seed' + name;
        slabel = etree.SubElement(sthingDef, 'label')
        slabel.text = name.lower() + ' seeds'
        splant = etree.SubElement(sthingDef, 'plant')
        splant.text = defName.text
        sstatBases = etree.SubElement(sthingDef, 'statBases')
        sMarketValue = etree.SubElement(sstatBases, 'MarketValue')
        sMarketValue.text = '0'
        sseed = etree.SubElement(sthingDef, 'seed')
        sharvestFactor = etree.SubElement(sseed, 'harvestFactor')
        sharvestFactor.text = '1.0'
        sseedFactor = etree.SubElement(sseed, 'seedFactor')
        sseedFactor.text = '1.0'
        sbaseChance = etree.SubElement(sseed, 'baseChance')
        sbaseChance.text = '1.0'
        sextraChance = etree.SubElement(sseed, 'extraChance')
        sextraChance.text = '0.1'
        
        harvestedThingDef = plant.find('harvestedThingDef')
        if harvestedThingDef is None:
            continue
        
        rthingDef = etree.SubElement(recipes, 'RecipeDef')
        rthingDef.set('ParentName', 'ExtractSeed')
        rdefName = etree.SubElement(rthingDef, 'defName')
        rdefName.text = 'ExtractSeed' + name;
        rlabel = etree.SubElement(rthingDef, 'label')
        rlabel.text = 'extract ' + name.lower() + ' seeds'
        rdesc = etree.SubElement(rthingDef, 'description')
        rdesc.text = 'Extract seeds from ' + harvestedThingDef.text.replace('Raw', '') + '.'
        ringredients = etree.XML('<ingredients><li><filter><thingDefs><li>' + harvestedThingDef.text + '</li></thingDefs></filter><count>25</count></li></ingredients>')
        rthingDef.append(ringredients);
        rfixedIngredientsFilter = etree.XML('<fixedIngredientFilter><thingDefs><li>' + harvestedThingDef.text + '</li></thingDefs></fixedIngredientFilter>')
        rthingDef.append(rfixedIngredientsFilter);
        rproducts = etree.SubElement(rthingDef, 'products')
        rproduct = etree.SubElement(rproducts, sdefName.text)
        rproduct.text = '5'
        

etree.ElementTree(seeds).write('Defs/ThingDefs/output_seeds.xml', pretty_print=True)
etree.ElementTree(recipes).write('Defs/RecipeDefs/output_recipes.xml', pretty_print=True)
