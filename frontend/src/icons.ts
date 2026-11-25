import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
  faBriefcase, faBuilding, faCar, faCircleCheck, faCouch, faDoorOpen, faDoorClosed, faElevator, faHouse,
  faHouseChimneyWindow, faKitchenSet, faPersonSwimming, faShieldHalved, faShirt, faShower, faSquareParking, faStairs,
  faSun, faTemperatureLow, faWifi, faSnowflake,
} from '@fortawesome/free-solid-svg-icons';
import type { PropertyType } from './api/types';

export const propertyTypeIcon: Record<PropertyType, IconDefinition> = {
  Room: faDoorClosed,
  Apartment: faBuilding,
  House: faHouse,
  Villa: faHouseChimneyWindow,
  Office: faBriefcase,
};

export const propertyTypeHint: Record<PropertyType, string> = {
  Room: 'Tiết kiệm, phù hợp sinh viên & người đi làm',
  Apartment: 'Tiện nghi, an ninh, có thang máy',
  House: 'Rộng rãi, riêng tư cho gia đình',
  Villa: 'Sang trọng, sân vườn, hồ bơi',
  Office: 'Mặt bằng làm việc, kinh doanh',
};

const AMENITY_ICONS: [RegExp, IconDefinition][] = [
  [/wifi|internet/i, faWifi],
  [/điều hòa|máy lạnh/i, faSnowflake],
  [/máy giặt/i, faShirt],
  [/nóng lạnh|bình nóng|tắm/i, faShower],
  [/tủ lạnh/i, faTemperatureLow],
  [/bếp/i, faKitchenSet],
  [/thang máy/i, faElevator],
  [/gara|ô tô/i, faCar],
  [/đỗ xe|để xe|gửi xe|parking/i, faSquareParking],
  [/ban công/i, faDoorOpen],
  [/sân thượng|sân vườn/i, faSun],
  [/gác/i, faStairs],
  [/nội thất|sofa/i, faCouch],
  [/bảo vệ|an ninh|camera/i, faShieldHalved],
  [/hồ bơi|bể bơi/i, faPersonSwimming],
];

export function amenityIcon(name: string): IconDefinition {
  return AMENITY_ICONS.find(([re]) => re.test(name))?.[1] ?? faCircleCheck;
}
