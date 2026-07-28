/**
 * The whole of the signed-in identity (v2.10/M56). Authorization is flat — on the whitelist means
 * full access — so there is deliberately nothing here resembling a role or a permission set. If this
 * ever grows a field, check it isn't a role in disguise.
 */
export interface AuthUser {
  email: string;
  displayName: string | null;
}
