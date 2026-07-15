import { http, buildQueryString } from "@/lib/http";
import type { Paged, ItemSummary, PageQuery } from "@/lib/types";

export async function fetchItems(
  params: PageQuery,
): Promise<Paged<ItemSummary>> {
  return http<Paged<ItemSummary>>(`/items${buildQueryString(params)}`);
}
